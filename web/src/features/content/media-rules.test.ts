import { describe, expect, it } from 'vitest'
import { formatForImages, mediaProblem } from './media-rules'

describe('media rules', () => {
  it('asks for an image before a single-image post can be reviewed', () => {
    expect(mediaProblem('SingleImage', 0)).toMatchObject({ key: 'needsMedia', min: 1 })
    expect(mediaProblem('SingleImage', 1)).toBeNull()
  })

  it('needs two to ten images for a carousel', () => {
    expect(mediaProblem('Carousel', 1)?.key).toBe('needsMedia')
    expect(mediaProblem('Carousel', 11)?.key).toBe('tooManyMedia')
    expect(mediaProblem('Carousel', 3)).toBeNull()
  })

  it('does not allow images on a text post', () => {
    expect(mediaProblem('Text', 1)?.key).toBe('noMediaAllowed')
  })

  it('picks the format from the number of images', () => {
    expect(formatForImages(0)).toBe('Text')
    expect(formatForImages(1)).toBe('SingleImage')
    expect(formatForImages(4)).toBe('Carousel')
  })
})
