const memory = new Map<string, string>();

export const storage = {
  async getItem(key: string) { return memory.get(key) ?? null; },
  async setItem(key: string, value: string) { memory.set(key, value); },
  async removeItem(key: string) { memory.delete(key); },
};
export default storage;
