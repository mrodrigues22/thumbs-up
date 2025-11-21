/**
 * ImageCropper Component
 * Modal component for cropping images with preview
 */

import { useState, useCallback } from 'react';
import Cropper from 'react-easy-crop';
import { Button } from './Button';

interface ImageCropperProps {
  image: string;
  onCropComplete: (croppedImage: Blob) => void;
  onCancel: () => void;
  aspectRatio?: number;
  cropShape?: 'rect' | 'round';
}

interface CropArea {
  x: number;
  y: number;
  width: number;
  height: number;
}

export const ImageCropper: React.FC<ImageCropperProps> = ({
  image,
  onCropComplete,
  onCancel,
  aspectRatio = 1,
  cropShape = 'round',
}) => {
  const [crop, setCrop] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<CropArea | null>(null);
  const [loading, setLoading] = useState(false);

  const onCropChange = (crop: { x: number; y: number }) => {
    setCrop(crop);
  };

  const onZoomChange = (zoom: number) => {
    setZoom(zoom);
  };

  const onCropAreaChange = useCallback(
    (_croppedArea: CropArea, croppedAreaPixels: CropArea) => {
      setCroppedAreaPixels(croppedAreaPixels);
    },
    []
  );

  const createCroppedImage = async () => {
    if (!croppedAreaPixels) return;

    setLoading(true);
    try {
      const croppedImage = await getCroppedImg(image, croppedAreaPixels);
      onCropComplete(croppedImage);
    } catch (error) {
      console.error('Error cropping image:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl mx-4">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">Crop Image</h2>
          <p className="text-sm text-gray-600 mt-1">
            Adjust the image position and zoom to crop
          </p>
        </div>

        {/* Cropper Area */}
        <div className="relative h-96 bg-gray-100">
          <Cropper
            image={image}
            crop={crop}
            zoom={zoom}
            aspect={aspectRatio}
            cropShape={cropShape}
            showGrid={true}
            onCropChange={onCropChange}
            onZoomChange={onZoomChange}
            onCropComplete={onCropAreaChange}
          />
        </div>

        {/* Zoom Control */}
        <div className="px-6 py-4 border-b border-gray-200">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Zoom
          </label>
          <input
            type="range"
            min={1}
            max={3}
            step={0.1}
            value={zoom}
            onChange={(e) => setZoom(Number(e.target.value))}
            className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer"
          />
        </div>

        {/* Actions */}
        <div className="px-6 py-4 flex justify-end space-x-3">
          <Button
            onClick={onCancel}
            variant="ghost"
            disabled={loading}
          >
            Cancel
          </Button>
          <Button
            onClick={createCroppedImage}
            variant="primary"
            disabled={loading}
          >
            {loading ? 'Processing...' : 'Apply Crop'}
          </Button>
        </div>
      </div>
    </div>
  );
};

// Helper function to create cropped image
const getCroppedImg = async (
  imageSrc: string,
  cropArea: CropArea
): Promise<Blob> => {
  const image = await createImage(imageSrc);
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d');

  if (!ctx) {
    throw new Error('Failed to get canvas context');
  }

  // Set canvas size to crop size
  canvas.width = cropArea.width;
  canvas.height = cropArea.height;

  // Draw the cropped image
  ctx.drawImage(
    image,
    cropArea.x,
    cropArea.y,
    cropArea.width,
    cropArea.height,
    0,
    0,
    cropArea.width,
    cropArea.height
  );

  // Convert canvas to blob
  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (blob) {
        resolve(blob);
      } else {
        reject(new Error('Failed to create blob'));
      }
    }, 'image/jpeg', 0.95);
  });
};

// Helper function to create image element
const createImage = (url: string): Promise<HTMLImageElement> => {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.addEventListener('load', () => resolve(image));
    image.addEventListener('error', (error) => reject(error));
    image.src = url;
  });
};
