import {
  generateKeyPair,
  encryptField,
  decryptField,
  KeyManager,
  EncryptedField,
} from '../encryption';

// Mock Web Crypto API
const mockCrypto = {
  subtle: {
    generateKey: jest.fn(),
    exportKey: jest.fn(),
    importKey: jest.fn(),
    encrypt: jest.fn(),
    decrypt: jest.fn(),
  },
  getRandomValues: jest.fn((arr: Uint8Array) => {
    for (let i = 0; i < arr.length; i++) {
      arr[i] = Math.floor(Math.random() * 256);
    }
    return arr;
  }),
};

Object.defineProperty(global, 'crypto', {
  value: mockCrypto,
  writable: true,
});

describe('Encryption Library', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('generateKeyPair', () => {
    it('should generate RSA-2048 key pair', async () => {
      const mockPublicKey = { type: 'public' };
      const mockPrivateKey = { type: 'private' };

      mockCrypto.subtle.generateKey.mockResolvedValue({
        publicKey: mockPublicKey,
        privateKey: mockPrivateKey,
      });

      mockCrypto.subtle.exportKey.mockImplementation((format: string) => {
        if (format === 'spki') {
          return Promise.resolve(new ArrayBuffer(294)); // Public key size
        } else {
          return Promise.resolve(new ArrayBuffer(1218)); // Private key size
        }
      });

      const keyPair = await generateKeyPair();

      expect(mockCrypto.subtle.generateKey).toHaveBeenCalledWith(
        {
          name: 'RSA-OAEP',
          modulusLength: 2048,
          publicExponent: new Uint8Array([0x01, 0x00, 0x01]),
          hash: { name: 'SHA-256' },
        },
        true,
        ['encrypt', 'decrypt']
      );

      expect(keyPair).toHaveProperty('publicKey');
      expect(keyPair).toHaveProperty('privateKey');
      expect(typeof keyPair.publicKey).toBe('string');
      expect(typeof keyPair.privateKey).toBe('string');
    });

    it('should export keys in base64 format', async () => {
      mockCrypto.subtle.generateKey.mockResolvedValue({
        publicKey: { type: 'public' },
        privateKey: { type: 'private' },
      });

      mockCrypto.subtle.exportKey.mockImplementation((format: string) => {
        if (format === 'spki') {
          const buffer = new ArrayBuffer(294);
          new Uint8Array(buffer).fill(65); // Fill with 'A' in ASCII
          return Promise.resolve(buffer);
        } else {
          const buffer = new ArrayBuffer(1218);
          new Uint8Array(buffer).fill(66); // Fill with 'B' in ASCII
          return Promise.resolve(buffer);
        }
      });

      const keyPair = await generateKeyPair();

      // Base64 encoded strings should not contain raw binary
      expect(keyPair.publicKey).toMatch(/^[A-Za-z0-9+/]+=*$/);
      expect(keyPair.privateKey).toMatch(/^[A-Za-z0-9+/]+=*$/);
    });
  });

  describe('encryptField', () => {
    it('should encrypt data for multiple recipients', async () => {
      const mockAESKey = { type: 'secret' };
      const mockPublicKey = { type: 'public' };

      mockCrypto.subtle.generateKey.mockResolvedValue(mockAESKey);
      mockCrypto.subtle.importKey.mockResolvedValue(mockPublicKey);

      mockCrypto.subtle.encrypt.mockImplementation((algorithm: any) => {
        if (algorithm.name === 'AES-GCM') {
          const buffer = new ArrayBuffer(32);
          new Uint8Array(buffer).fill(1);
          return Promise.resolve(buffer);
        } else {
          const buffer = new ArrayBuffer(256);
          new Uint8Array(buffer).fill(2);
          return Promise.resolve(buffer);
        }
      });

      mockCrypto.subtle.exportKey.mockResolvedValue(new ArrayBuffer(32));

      const recipients = [
        { userId: 1, publicKey: btoa('public-key-1') }, // Properly encode as base64
        { userId: 2, publicKey: btoa('public-key-2') },
      ];

      const result = await encryptField('Test Data', recipients);

      expect(result).toHaveProperty('data');
      expect(result).toHaveProperty('iv');
      expect(result).toHaveProperty('keys');
      expect(result.keys).toHaveLength(2);
      expect(result.keys[0]).toHaveProperty('userId', 1);
      expect(result.keys[0]).toHaveProperty('encryptedKey');
      expect(result.keys[1]).toHaveProperty('userId', 2);
      expect(result.keys[1]).toHaveProperty('encryptedKey');
    });

    it('should generate unique IV for each encryption', async () => {
      const mockAESKey = { type: 'secret' };
      const mockPublicKey = { type: 'public' };

      mockCrypto.subtle.generateKey.mockResolvedValue(mockAESKey);
      mockCrypto.subtle.importKey.mockResolvedValue(mockPublicKey);
      mockCrypto.subtle.encrypt.mockResolvedValue(new ArrayBuffer(32));
      mockCrypto.subtle.exportKey.mockResolvedValue(new ArrayBuffer(32));

      const recipients = [{ userId: 1, publicKey: btoa('public-key-1') }];

      const result1 = await encryptField('Test Data', recipients);
      const result2 = await encryptField('Test Data', recipients);

      // IVs should be different even for same data
      expect(result1.iv).not.toBe(result2.iv);
    });

    it('should use AES-256-GCM for data encryption', async () => {
      const mockAESKey = { type: 'secret' };
      const mockPublicKey = { type: 'public' };

      mockCrypto.subtle.generateKey.mockResolvedValue(mockAESKey);
      mockCrypto.subtle.importKey.mockResolvedValue(mockPublicKey);
      mockCrypto.subtle.encrypt.mockResolvedValue(new ArrayBuffer(32));
      mockCrypto.subtle.exportKey.mockResolvedValue(new ArrayBuffer(32));

      const recipients = [{ userId: 1, publicKey: btoa('public-key-1') }];

      await encryptField('Test Data', recipients);

      expect(mockCrypto.subtle.generateKey).toHaveBeenCalledWith(
        {
          name: 'AES-GCM',
          length: 256,
        },
        true,
        ['encrypt', 'decrypt']
      );
    });
  });

  describe('decryptField', () => {
    it('should decrypt field using user private key', async () => {
      const mockPrivateKey = { type: 'private' };
      const mockAESKey = { type: 'secret' };

      mockCrypto.subtle.importKey.mockResolvedValue(mockPrivateKey);
      mockCrypto.subtle.decrypt.mockImplementation((algorithm: any) => {
        if (algorithm.name === 'RSA-OAEP') {
          const buffer = new ArrayBuffer(32);
          return Promise.resolve(buffer);
        } else {
          const text = 'Decrypted Data';
          const encoder = new TextEncoder();
          return Promise.resolve(encoder.encode(text).buffer);
        }
      });

      mockCrypto.subtle.importKey.mockImplementation((format: string) => {
        if (format === 'pkcs8') {
          return Promise.resolve(mockPrivateKey);
        } else {
          return Promise.resolve(mockAESKey);
        }
      });

      const encryptedField: EncryptedField = {
        data: btoa('encrypted-data'),
        iv: btoa('initialization-vector'),
        keys: [
          { userId: 1, encryptedKey: btoa('encrypted-key-1') },
          { userId: 2, encryptedKey: btoa('encrypted-key-2') },
        ],
      };

      const result = await decryptField(encryptedField, 1, btoa('private-key-base64'));

      expect(result).toBe('Decrypted Data');
    });

    it('should throw error if user key not found', async () => {
      const encryptedField: EncryptedField = {
        data: 'encrypted-data',
        iv: 'initialization-vector',
        keys: [
          { userId: 1, encryptedKey: 'encrypted-key-1' },
          { userId: 2, encryptedKey: 'encrypted-key-2' },
        ],
      };

      await expect(
        decryptField(encryptedField, 999, 'private-key-base64')
      ).rejects.toThrow('No encryption key found for this user');
    });

    it('should import private key correctly', async () => {
      const mockPrivateKey = { type: 'private' };
      const mockAESKey = { type: 'secret' };

      mockCrypto.subtle.importKey.mockImplementation((format: string) => {
        if (format === 'pkcs8') {
          return Promise.resolve(mockPrivateKey);
        } else {
          return Promise.resolve(mockAESKey);
        }
      });

      mockCrypto.subtle.decrypt.mockImplementation(() => {
        const text = 'Decrypted Data';
        const encoder = new TextEncoder();
        return Promise.resolve(encoder.encode(text).buffer);
      });

      const encryptedField: EncryptedField = {
        data: btoa('encrypted-data'),
        iv: btoa('initialization-vector'),
        keys: [{ userId: 1, encryptedKey: btoa('encrypted-key-1') }],
      };

      await decryptField(encryptedField, 1, btoa('private-key-base64'));

      expect(mockCrypto.subtle.importKey).toHaveBeenCalledWith(
        'pkcs8',
        expect.any(ArrayBuffer),
        {
          name: 'RSA-OAEP',
          hash: { name: 'SHA-256' },
        },
        true,
        ['decrypt']
      );
    });
  });

  describe('KeyManager', () => {
    beforeEach(() => {
      KeyManager.clearPrivateKey();
    });

    it('should store and retrieve private key from memory', () => {
      const privateKey = 'test-private-key';

      KeyManager.setPrivateKey(privateKey);
      const retrieved = KeyManager.getPrivateKey();

      expect(retrieved).toBe(privateKey);
    });

    it('should return null when no key is stored', () => {
      const retrieved = KeyManager.getPrivateKey();
      expect(retrieved).toBeNull();
    });

    it('should clear private key from memory', () => {
      KeyManager.setPrivateKey('test-private-key');
      KeyManager.clearPrivateKey();

      const retrieved = KeyManager.getPrivateKey();
      expect(retrieved).toBeNull();
    });

    it('should download private key as file', () => {
      const privateKey = 'test-private-key-content';
      const username = 'testuser';

      // Create mock element
      const mockElement = {
        href: '',
        download: '',
        click: jest.fn(),
      };

      // Mock DOM methods
      const mockCreateElement = jest.fn(() => mockElement);
      const mockAppendChild = jest.fn();
      const mockRemoveChild = jest.fn();
      const mockCreateObjectURL = jest.fn(() => 'blob:mock-url');
      const mockRevokeObjectURL = jest.fn();

      // Setup mocks
      document.createElement = mockCreateElement as any;
      document.body.appendChild = mockAppendChild;
      document.body.removeChild = mockRemoveChild;
      global.URL.createObjectURL = mockCreateObjectURL;
      global.URL.revokeObjectURL = mockRevokeObjectURL;

      KeyManager.downloadPrivateKey(privateKey, username);

      expect(mockCreateObjectURL).toHaveBeenCalledWith(expect.any(Blob));
      expect(mockElement.click).toHaveBeenCalled();
      expect(mockRevokeObjectURL).toHaveBeenCalledWith('blob:mock-url');
    });

    it('should load private key from file', async () => {
      const keyContent = 'private-key-from-file';
      const file = new File([keyContent], 'private.key', { type: 'text/plain' });

      const result = await KeyManager.loadPrivateKeyFromFile(file);

      expect(result).toBe(keyContent);
      expect(KeyManager.getPrivateKey()).toBe(keyContent);
    });

    it('should handle file read errors', async () => {
      const file = new File([''], 'private.key', { type: 'text/plain' });

      // Mock FileReader to simulate error
      const mockFileReader = {
        readAsText: function (this: any) {
          setTimeout(() => {
            if (this.onerror) {
              this.onerror(new Error('Read error'));
            }
          }, 0);
        },
        onerror: null,
        onload: null,
      };

      global.FileReader = jest.fn(() => mockFileReader) as any;

      await expect(KeyManager.loadPrivateKeyFromFile(file)).rejects.toThrow();
    });
  });

  describe('Base64 Conversion', () => {
    it('should correctly convert ArrayBuffer to base64', () => {
      // This is tested implicitly through generateKeyPair
      // We verify that the output is valid base64
      mockCrypto.subtle.generateKey.mockResolvedValue({
        publicKey: { type: 'public' },
        privateKey: { type: 'private' },
      });

      mockCrypto.subtle.exportKey.mockImplementation(() => {
        const buffer = new ArrayBuffer(100);
        const view = new Uint8Array(buffer);
        for (let i = 0; i < view.length; i++) {
          view[i] = i % 256;
        }
        return Promise.resolve(buffer);
      });

      return generateKeyPair().then((keyPair) => {
        // Verify base64 format
        expect(keyPair.publicKey).toMatch(/^[A-Za-z0-9+/]+=*$/);
        expect(keyPair.privateKey).toMatch(/^[A-Za-z0-9+/]+=*$/);

        // Verify not empty
        expect(keyPair.publicKey.length).toBeGreaterThan(0);
        expect(keyPair.privateKey.length).toBeGreaterThan(0);
      });
    });
  });

  describe('Integration Tests', () => {
    it('should encrypt and decrypt data successfully', async () => {
      // This tests the full encryption/decryption cycle
      const mockAESKey = { type: 'secret' };
      const mockPublicKey = { type: 'public' };
      const mockPrivateKey = { type: 'private' };

      // Setup encryption mocks
      mockCrypto.subtle.generateKey.mockResolvedValue(mockAESKey);
      mockCrypto.subtle.exportKey.mockResolvedValue(new ArrayBuffer(32));

      let encryptedData: ArrayBuffer;
      let encryptedKey: ArrayBuffer;

      mockCrypto.subtle.encrypt.mockImplementation((algorithm: any, key: any, data: any) => {
        if (algorithm.name === 'AES-GCM') {
          const buffer = new ArrayBuffer(50);
          encryptedData = buffer;
          return Promise.resolve(buffer);
        } else {
          const buffer = new ArrayBuffer(256);
          encryptedKey = buffer;
          return Promise.resolve(buffer);
        }
      });

      mockCrypto.subtle.importKey.mockImplementation((format: string) => {
        if (format === 'spki') {
          return Promise.resolve(mockPublicKey);
        } else if (format === 'pkcs8') {
          return Promise.resolve(mockPrivateKey);
        } else {
          return Promise.resolve(mockAESKey);
        }
      });

      // Encrypt
      const recipients = [{ userId: 1, publicKey: btoa('public-key-1') }];
      const encrypted = await encryptField('Secret Message', recipients);

      // Setup decryption mocks
      mockCrypto.subtle.decrypt.mockImplementation((algorithm: any) => {
        if (algorithm.name === 'RSA-OAEP') {
          return Promise.resolve(new ArrayBuffer(32));
        } else {
          const text = 'Secret Message';
          const encoder = new TextEncoder();
          return Promise.resolve(encoder.encode(text).buffer);
        }
      });

      // Decrypt
      const decrypted = await decryptField(encrypted, 1, btoa('private-key-1'));

      expect(decrypted).toBe('Secret Message');
    });
  });
});
