import { ProcessRequest, ProcessResponse, PreviewRequest, PreviewResponse, ApiError } from '../types/api';

class ApiService {
    private baseUrl: string;

    constructor() {
        this.baseUrl = 'http://localhost:5000/api';
    }

    async processFiles(request: ProcessRequest): Promise<ProcessResponse> {
        const response = await fetch(`${this.baseUrl}/fileprocessing/process`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        });

        if (!response.ok) {
            const error: ApiError = await response.json();
            throw new Error(error.error);
        }

        return response.json();
    }

    async previewFileName(request: PreviewRequest): Promise<PreviewResponse> {
        const response = await fetch(`${this.baseUrl}/fileprocessing/preview`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        });

        if (!response.ok) {
            const error: ApiError = await response.json();
            throw new Error(error.error);
        }

        return response.json();
    }
}

export const apiService = new ApiService(); 