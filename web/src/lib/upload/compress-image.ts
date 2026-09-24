import { calculateTargetDimensions } from './image-dimensions'

const MAX_DIMENSION = 1600
const JPEG_QUALITY = 0.8

// PDFs and anything not an image pass through untouched — compression only
// applies to photos, which is what actually blows up on a phone camera.
export async function compressImage(file: File): Promise<File> {
  if (!file.type.startsWith('image/')) {
    return file
  }

  const bitmap = await createImageBitmap(file)
  try {
    const { width, height } = calculateTargetDimensions(bitmap.width, bitmap.height, MAX_DIMENSION)

    const canvas = document.createElement('canvas')
    canvas.width = width
    canvas.height = height

    const context = canvas.getContext('2d')
    if (!context) {
      return file
    }

    context.drawImage(bitmap, 0, 0, width, height)

    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', JPEG_QUALITY))
    if (!blob || blob.size >= file.size) {
      return file
    }

    const compressedName = `${file.name.replace(/\.[^./]+$/, '')}.jpg`
    return new File([blob], compressedName, { type: 'image/jpeg' })
  } finally {
    bitmap.close()
  }
}
