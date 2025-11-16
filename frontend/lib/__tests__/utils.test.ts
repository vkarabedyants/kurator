import { cn } from '../utils';

describe('cn utility function', () => {
  it('should merge class names correctly', () => {
    const result = cn('foo', 'bar');
    expect(result).toContain('foo');
    expect(result).toContain('bar');
  });

  it('should handle conditional classes', () => {
    const result = cn('foo', false && 'bar', 'baz');
    expect(result).toContain('foo');
    expect(result).not.toContain('bar');
    expect(result).toContain('baz');
  });

  it('should merge Tailwind classes correctly', () => {
    const result = cn('px-2', 'px-4');
    // twMerge should keep only the last px class
    expect(result).toBe('px-4');
  });

  it('should handle empty input', () => {
    const result = cn();
    expect(result).toBe('');
  });

  it('should handle array of classes', () => {
    const result = cn(['foo', 'bar']);
    expect(result).toContain('foo');
    expect(result).toContain('bar');
  });

  it('should handle objects with conditional classes', () => {
    const result = cn({
      'foo': true,
      'bar': false,
      'baz': true,
    });
    expect(result).toContain('foo');
    expect(result).not.toContain('bar');
    expect(result).toContain('baz');
  });

  it('should merge conflicting Tailwind classes', () => {
    const result = cn('bg-red-500', 'bg-blue-500');
    expect(result).toBe('bg-blue-500');
  });

  it('should handle multiple types of inputs', () => {
    const result = cn(
      'base-class',
      { 'conditional': true },
      ['array-class'],
      false && 'hidden-class'
    );
    expect(result).toContain('base-class');
    expect(result).toContain('conditional');
    expect(result).toContain('array-class');
    expect(result).not.toContain('hidden-class');
  });
});
