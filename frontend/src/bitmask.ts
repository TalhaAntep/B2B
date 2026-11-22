/** Display-only bit test. Writes send option operations to the API for server-side calculation. */
export function isOptionEnabled(current: string | null, optionValue: string): boolean {
  if (current === null) return false;
  try {
    return (BigInt(current) & BigInt(optionValue)) !== 0n;
  } catch {
    return false;
  }
}

export function bitOperationKey(parameterId: string, optionValue: string): string {
  return `${parameterId}\u0000${optionValue}`;
}
