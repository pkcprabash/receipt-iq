// Parses a user-typed money amount into a number rounded to cents.
// Returns null for anything that isn't a plain decimal ("12", "-3.5", "1,234.50").
export function parseAmount(input: string): number | null {
  const cleaned = input.trim().replace(/,/g, '')
  if (!/^-?\d+(\.\d+)?$/.test(cleaned)) {
    return null
  }

  // The exponent shift avoids float artifacts like 1.005 * 100 = 100.49999…
  const magnitude = Number(`${Math.round(Number(`${cleaned.replace('-', '')}e2`))}e-2`)
  return cleaned.startsWith('-') ? -magnitude : magnitude
}
