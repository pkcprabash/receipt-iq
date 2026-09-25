import { describe, expect, it } from 'vitest'
import { parseAmount } from './parse-amount'

describe('parseAmount', () => {
  it('parses plain and negative decimals', () => {
    expect(parseAmount('12')).toBe(12)
    expect(parseAmount(' 3.5 ')).toBe(3.5)
    expect(parseAmount('-2.25')).toBe(-2.25)
  })

  it('strips thousands separators and rounds to cents', () => {
    expect(parseAmount('1,234.50')).toBe(1234.5)
    expect(parseAmount('1.005')).toBe(1.01)
  })

  it('rejects non-numeric input', () => {
    expect(parseAmount('')).toBeNull()
    expect(parseAmount('abc')).toBeNull()
    expect(parseAmount('$5')).toBeNull()
    expect(parseAmount('1.2.3')).toBeNull()
  })
})
