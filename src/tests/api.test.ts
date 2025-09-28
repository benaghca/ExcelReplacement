import '@jest/globals';
import { apiService } from '../services/api';
import { ProcessRequest, PreviewRequest } from '../types/api';
import { resetMocks } from './testUtils';

describe('API Service', () => {
    beforeEach(() => {
        resetMocks();
    });

    describe('processFiles', () => {
        const mockRequest: ProcessRequest = {
            csvFilePath: 'test.csv',
            templatePath: 'template.xlsx',
            outputPath: 'output',
            templateType: 'excel',
            fileNamePattern: '[Test] [Pattern]'
        };

        test('should successfully process files', async () => {
            const mockResponse = {
                success: true,
                processedFiles: 5
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve(mockResponse)
            });

            const result = await apiService.processFiles(mockRequest);
            expect(result).toEqual(mockResponse);
            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/fileprocessing/process',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(mockRequest)
                }
            );
        });

        test('should handle API errors', async () => {
            const mockError = { error: 'Processing failed' };
            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: false,
                json: () => Promise.resolve(mockError)
            });

            await expect(apiService.processFiles(mockRequest)).rejects.toThrow('Processing failed');
        });

        test('should handle network errors', async () => {
            (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));
            await expect(apiService.processFiles(mockRequest)).rejects.toThrow('Network error');
        });
    });

    describe('previewFileName', () => {
        const mockRequest: PreviewRequest = {
            csvFilePath: 'test.csv',
            fileNamePattern: '[Test] [Pattern]'
        };

        test('should successfully generate preview', async () => {
            const mockResponse = {
                previewName: 'Test Pattern.xlsx',
                fields: {
                    Location: 'Test',
                    Facility: 'Pattern'
                }
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve(mockResponse)
            });

            const result = await apiService.previewFileName(mockRequest);
            expect(result).toEqual(mockResponse);
            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/fileprocessing/preview',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(mockRequest)
                }
            );
        });

        test('should handle API errors', async () => {
            const mockError = { error: 'Preview generation failed' };
            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: false,
                json: () => Promise.resolve(mockError)
            });

            await expect(apiService.previewFileName(mockRequest)).rejects.toThrow('Preview generation failed');
        });

        test('should handle network errors', async () => {
            (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));
            await expect(apiService.previewFileName(mockRequest)).rejects.toThrow('Network error');
        });
    });
}); 