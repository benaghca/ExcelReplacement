import '@jest/globals';

// Mock electron
jest.mock('electron', () => ({
    ipcRenderer: {
        invoke: jest.fn()
    }
}));

// Mock fetch
global.fetch = jest.fn();

// Mock document.getElementById
document.getElementById = jest.fn();

export const resetMocks = () => {
    jest.clearAllMocks();
    // Reset document.getElementById mock
    (document.getElementById as jest.Mock).mockReset();
}; 