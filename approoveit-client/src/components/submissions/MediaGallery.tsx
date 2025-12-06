/**
 * MediaGallery Component
 * Displays media files in a gallery format
 */

import { useState } from 'react';
import type { MediaFileResponse } from '../../shared/types';
import { MediaFileType } from '../../shared/types';
import { Modal } from '../common';

interface MediaGalleryProps {
  mediaFiles: MediaFileResponse[];
}

export const MediaGallery: React.FC<MediaGalleryProps> = ({ mediaFiles }) => {
  const [selectedMedia, setSelectedMedia] = useState<MediaFileResponse | null>(null);

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  return (
    <>
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {mediaFiles.map((file) => (
          <div
            key={file.id}
            className="relative group cursor-pointer rounded-lg overflow-hidden bg-gray-100 aspect-square"
            onClick={() => setSelectedMedia(file)}
          >
            {file.fileType === MediaFileType.Image ? (
              <img
                src={file.fileUrl}
                alt={file.fileName}
                className="w-full h-full object-cover transition-transform group-hover:scale-105"
              />
            ) : (
              <video
                src={file.fileUrl}
                className="w-full h-full object-cover"
                preload="metadata"
              />
            )}
            
            {/* Overlay */}
            <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-40 transition-opacity flex items-center justify-center">
              <svg
                className="w-12 h-12 text-white opacity-0 group-hover:opacity-100 transition-opacity"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                />
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                />
              </svg>
            </div>
            
            {/* File Type Badge */}
            <div className="absolute top-2 right-2 bg-black bg-opacity-50 text-white text-xs px-2 py-1 rounded">
              {file.fileType === MediaFileType.Image ? 'Image' : 'Video'}
            </div>
          </div>
        ))}
      </div>

      {/* Media Preview Modal */}
      {selectedMedia && (
        <Modal
          isOpen={true}
          onClose={() => setSelectedMedia(null)}
          title={selectedMedia.fileName}
          className="max-w-4xl"
        >
          <div className="space-y-4">
            {selectedMedia.fileType === MediaFileType.Image ? (
              <img
                src={selectedMedia.fileUrl}
                alt={selectedMedia.fileName}
                className="w-full rounded-lg"
              />
            ) : (
              <video
                src={selectedMedia.fileUrl}
                controls
                className="w-full rounded-lg"
              />
            )}
            
            <div className="text-sm text-gray-600 space-y-1">
              <p><strong>File Size:</strong> {formatFileSize(selectedMedia.fileSize)}</p>
              <p>
                <strong>Uploaded:</strong>{' '}
                {new Date(selectedMedia.uploadedAt).toLocaleString()}
              </p>
            </div>
            
            <a
              href={selectedMedia.fileUrl}
              download={selectedMedia.fileName}
              className="inline-block btn-primary"
            >
              Download
            </a>
          </div>
        </Modal>
      )}
    </>
  );
};
