import { describe, expect, it } from 'vitest'
import { calculateTargetDimensions } from './image-dimensions'

describe('calculateTargetDimensions', () => {
  it('leaves an image already within bounds unchanged', () => {
    expect(calculateTargetDimensions(800, 600, 1600)).toEqual({ width: 800, height: 600 })
  })

  it('never scales an image up', () => {
    expect(calculateTargetDimensions(400, 300, 1600)).toEqual({ width: 400, height: 300 })
  })

  it('scales down a landscape image to fit the longest side', () => {
    expect(calculateTargetDimensions(4000, 2000, 1600)).toEqual({ width: 1600, height: 800 })
  })

  it('scales down a portrait image to fit the longest side', () => {
    expect(calculateTargetDimensions(2000, 4000, 1600)).toEqual({ width: 800, height: 1600 })
  })

  it('handles a square image at the exact limit', () => {
    expect(calculateTargetDimensions(1600, 1600, 1600)).toEqual({ width: 1600, height: 1600 })
  })
})
