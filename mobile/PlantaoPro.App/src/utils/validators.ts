export const isRequired = (value?: string | null) => Boolean(value && value.trim().length > 0);
export const isEmail = (value?: string | null) => Boolean(value && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value));
export const minLength = (value: string | undefined | null, length: number) => Boolean(value && value.length >= length);
