/**
 * Client-side RSA-2048 encryption library for KURATOR
 * Implements end-to-end encryption using Web Crypto API
 */

// Helper functions for converting between formats
function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (let i = 0; i < bytes.byteLength; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return btoa(binary);
}

function base64ToArrayBuffer(base64: string): ArrayBuffer {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes.buffer;
}

/**
 * Generate RSA-2048 key pair for a user
 */
export async function generateKeyPair(): Promise<{
  publicKey: string;
  privateKey: string;
}> {
  const keyPair = await crypto.subtle.generateKey(
    {
      name: 'RSA-OAEP',
      modulusLength: 2048,
      publicExponent: new Uint8Array([0x01, 0x00, 0x01]),
      hash: { name: 'SHA-256' },
    },
    true,
    ['encrypt', 'decrypt']
  );

  // Export public key
  const publicKeyData = await crypto.subtle.exportKey('spki', keyPair.publicKey);
  const publicKey = arrayBufferToBase64(publicKeyData);

  // Export private key
  const privateKeyData = await crypto.subtle.exportKey('pkcs8', keyPair.privateKey);
  const privateKey = arrayBufferToBase64(privateKeyData);

  return { publicKey, privateKey };
}

/**
 * Generate random AES-256 key for symmetric encryption
 */
async function generateAESKey(): Promise<CryptoKey> {
  return await crypto.subtle.generateKey(
    {
      name: 'AES-GCM',
      length: 256,
    },
    true,
    ['encrypt', 'decrypt']
  );
}

/**
 * Encrypt data with AES-256-GCM
 */
async function encryptWithAES(data: string, key: CryptoKey): Promise<{ encrypted: string; iv: string }> {
  const encoder = new TextEncoder();
  const encodedData = encoder.encode(data);

  // Generate random initialization vector
  const iv = crypto.getRandomValues(new Uint8Array(12));

  const encrypted = await crypto.subtle.encrypt(
    {
      name: 'AES-GCM',
      iv: iv,
    },
    key,
    encodedData
  );

  return {
    encrypted: arrayBufferToBase64(encrypted),
    iv: arrayBufferToBase64(iv.buffer),
  };
}

/**
 * Decrypt data with AES-256-GCM
 */
async function decryptWithAES(encryptedData: string, key: CryptoKey, iv: string): Promise<string> {
  const decoder = new TextDecoder();

  const decrypted = await crypto.subtle.decrypt(
    {
      name: 'AES-GCM',
      iv: base64ToArrayBuffer(iv),
    },
    key,
    base64ToArrayBuffer(encryptedData)
  );

  return decoder.decode(decrypted);
}

/**
 * Import RSA public key from base64 string
 */
async function importPublicKey(publicKeyBase64: string): Promise<CryptoKey> {
  const publicKeyData = base64ToArrayBuffer(publicKeyBase64);

  return await crypto.subtle.importKey(
    'spki',
    publicKeyData,
    {
      name: 'RSA-OAEP',
      hash: { name: 'SHA-256' },
    },
    true,
    ['encrypt']
  );
}

/**
 * Import RSA private key from base64 string
 */
async function importPrivateKey(privateKeyBase64: string): Promise<CryptoKey> {
  const privateKeyData = base64ToArrayBuffer(privateKeyBase64);

  return await crypto.subtle.importKey(
    'pkcs8',
    privateKeyData,
    {
      name: 'RSA-OAEP',
      hash: { name: 'SHA-256' },
    },
    true,
    ['decrypt']
  );
}

/**
 * Encrypt AES key with RSA public key
 */
async function encryptKeyWithRSA(aesKey: CryptoKey, publicKey: CryptoKey): Promise<string> {
  // Export AES key as raw data
  const aesKeyData = await crypto.subtle.exportKey('raw', aesKey);

  // Encrypt with RSA
  const encrypted = await crypto.subtle.encrypt(
    {
      name: 'RSA-OAEP',
    },
    publicKey,
    aesKeyData
  );

  return arrayBufferToBase64(encrypted);
}

/**
 * Decrypt AES key with RSA private key
 */
async function decryptKeyWithRSA(encryptedKey: string, privateKey: CryptoKey): Promise<CryptoKey> {
  const encryptedKeyData = base64ToArrayBuffer(encryptedKey);

  // Decrypt with RSA
  const aesKeyData = await crypto.subtle.decrypt(
    {
      name: 'RSA-OAEP',
    },
    privateKey,
    encryptedKeyData
  );

  // Import AES key
  return await crypto.subtle.importKey(
    'raw',
    aesKeyData,
    {
      name: 'AES-GCM',
      length: 256,
    },
    true,
    ['encrypt', 'decrypt']
  );
}

/**
 * Encrypted field data structure
 */
export interface EncryptedField {
  data: string;           // AES encrypted data
  iv: string;             // Initialization vector for AES
  keys: {                 // AES key encrypted for each recipient
    userId: number;
    encryptedKey: string; // RSA encrypted AES key
  }[];
}

/**
 * Encrypt a field for multiple recipients
 * @param data The data to encrypt
 * @param recipients Array of {userId, publicKey} for each recipient
 */
export async function encryptField(
  data: string,
  recipients: { userId: number; publicKey: string }[]
): Promise<EncryptedField> {
  // Generate AES key for this field
  const aesKey = await generateAESKey();

  // Encrypt data with AES
  const { encrypted, iv } = await encryptWithAES(data, aesKey);

  // Encrypt AES key for each recipient
  const encryptedKeys = await Promise.all(
    recipients.map(async (recipient) => {
      const publicKey = await importPublicKey(recipient.publicKey);
      const encryptedKey = await encryptKeyWithRSA(aesKey, publicKey);
      return {
        userId: recipient.userId,
        encryptedKey,
      };
    })
  );

  return {
    data: encrypted,
    iv,
    keys: encryptedKeys,
  };
}

/**
 * Decrypt a field using user's private key
 * @param encryptedField The encrypted field data
 * @param userId The current user's ID
 * @param privateKey The user's private key (base64)
 */
export async function decryptField(
  encryptedField: EncryptedField,
  userId: number,
  privateKey: string
): Promise<string> {
  // Find the encrypted key for this user
  const userKey = encryptedField.keys.find(k => k.userId === userId);
  if (!userKey) {
    throw new Error('No encryption key found for this user');
  }

  // Import private key
  const privKey = await importPrivateKey(privateKey);

  // Decrypt AES key
  const aesKey = await decryptKeyWithRSA(userKey.encryptedKey, privKey);

  // Decrypt data
  return await decryptWithAES(encryptedField.data, aesKey, encryptedField.iv);
}

/**
 * Key manager for storing and retrieving user's private key
 */
export class KeyManager {
  private static STORAGE_KEY = 'kurator_private_key';
  private static privateKey: string | null = null;

  /**
   * Store private key in memory (session only)
   */
  static setPrivateKey(key: string): void {
    this.privateKey = key;
  }

  /**
   * Get private key from memory
   */
  static getPrivateKey(): string | null {
    return this.privateKey;
  }

  /**
   * Clear private key from memory
   */
  static clearPrivateKey(): void {
    this.privateKey = null;
  }

  /**
   * Download private key as file
   */
  static downloadPrivateKey(privateKey: string, username: string): void {
    const blob = new Blob([privateKey], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `kurator_private_key_${username}.key`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  /**
   * Load private key from file
   */
  static async loadPrivateKeyFromFile(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        const key = e.target?.result as string;
        this.setPrivateKey(key);
        resolve(key);
      };
      reader.onerror = reject;
      reader.readAsText(file);
    });
  }
}

/**
 * Helper function to get recipients for a block
 * (Admin + all curators assigned to the block)
 */
export async function getBlockRecipients(
  blockId: number,
  usersApi: any
): Promise<{ userId: number; publicKey: string }[]> {
  // Get all users
  const users = await usersApi.getAll();

  // Get block curators
  const blockCurators = await usersApi.getBlockCurators(blockId);

  // Include admins and block curators
  const recipients = users
    .filter((user: any) =>
      user.role === 'Admin' ||
      blockCurators.some((bc: any) => bc.userId === user.id)
    )
    .map((user: any) => ({
      userId: user.id,
      publicKey: user.publicKey,
    }));

  return recipients;
}