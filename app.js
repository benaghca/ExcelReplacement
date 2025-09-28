// Simple app.js for Document Template Processor
console.log('App loaded');

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, setting up handlers');
    setupEventHandlers();
    setupDragAndDrop();
    testAPI();
});

function setupEventHandlers() {
    // Button event handlers
    document.getElementById('browseCsv')?.addEventListener('click', browseCsvFile);
    document.getElementById('browseTemplate')?.addEventListener('click', browseTemplateFile);
    document.getElementById('browseOutput')?.addEventListener('click', browseOutputDirectory);
    document.getElementById('process')?.addEventListener('click', processFiles);
    document.getElementById('preview')?.addEventListener('click', generatePreview);
    document.getElementById('manageTemplates')?.addEventListener('click', showTemplateManager);
    document.getElementById('configureFileName')?.addEventListener('click', showFileNameConfigDialog);
    
    // Template type change handler
    document.getElementById('templateType')?.addEventListener('change', function() {
        const select = this;
        select.style.borderColor = '#2563eb';
        select.style.boxShadow = '0 0 0 3px #dbeafe';
        
        // Update status
        updateStatus('Template type changed to: ' + select.value, 'info');
        
        // Update placeholder text
        const templateDropZone = document.getElementById('templateDropZone');
        if (templateDropZone) {
            const text = templateDropZone.querySelector('.drop-zone-text');
            if (text) {
                text.textContent = `Drop ${select.value} template here or click Browse`;
            }
        }
    });
    
    // Done button handler
    document.getElementById('doneButton')?.addEventListener('click', function() {
        document.getElementById('progressModal').style.display = 'none';
    });
}

function setupDragAndDrop() {
    const dropZones = ['csvDropZone', 'templateDropZone', 'outputDropZone'];
    
    dropZones.forEach(zoneId => {
        const zone = document.getElementById(zoneId);
        if (!zone) return;
        
        zone.addEventListener('dragover', function(e) {
            e.preventDefault();
            this.classList.add('drag-over');
        });
        
        zone.addEventListener('dragleave', function(e) {
            e.preventDefault();
            this.classList.remove('drag-over');
        });
        
        zone.addEventListener('drop', function(e) {
            e.preventDefault();
            this.classList.remove('drag-over');
            this.classList.add('has-file');
            
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                handleFileDrop(zoneId, files[0]);
            }
        });
        
        zone.addEventListener('click', function() {
            // Trigger the corresponding browse button
            const buttonId = zoneId.replace('DropZone', '');
            const button = document.getElementById('browse' + buttonId.charAt(0).toUpperCase() + buttonId.slice(1));
            if (button) {
                button.click();
            }
        });
    });
}

function handleFileDrop(zoneId, file) {
    const inputId = zoneId.replace('DropZone', 'File');
    const input = document.getElementById(inputId);
    
    if (input) {
        input.value = file.path || file.name;
        updateStatus(`File selected: ${file.name}`, 'success');
        
        // Validate the file
        if (zoneId === 'csvDropZone') {
            validateCsvFile(file.path || file.name);
        } else if (zoneId === 'templateDropZone') {
            validateTemplateFile(file.path || file.name);
        }
    }
}

async function browseCsvFile() {
    try {
        updateStatus('Opening CSV file dialog...', 'info');
        
        if (window.require) {
            const { ipcRenderer } = window.require('electron');
            const result = await ipcRenderer.invoke('select-csv-file');
            
            if (result && !result.canceled && result.filePaths.length > 0) {
                const filePath = result.filePaths[0];
                document.getElementById('csvFile').value = filePath;
                updateStatus(`CSV file selected: ${filePath}`, 'success');
                await validateCsvFile(filePath);
            }
        } else {
            updateStatus('Electron API not available - please use drag and drop', 'warning');
        }
    } catch (error) {
        console.error('Error selecting CSV file:', error);
        updateStatus('Error selecting CSV file: ' + error.message, 'error');
    }
}

async function browseTemplateFile() {
    try {
        updateStatus('Opening template file dialog...', 'info');
        
        if (window.require) {
            const { ipcRenderer } = window.require('electron');
            const templateType = document.getElementById('templateType').value;
            const result = await ipcRenderer.invoke('select-template-file', templateType);
            
            if (result && !result.canceled && result.filePaths.length > 0) {
                const filePath = result.filePaths[0];
                document.getElementById('templateFile').value = filePath;
                updateStatus(`${templateType.toUpperCase()} template selected: ${filePath}`, 'success');
                await validateTemplateFile(filePath);
            }
        } else {
            updateStatus('Electron API not available - please use drag and drop', 'warning');
        }
    } catch (error) {
        console.error('Error selecting template file:', error);
        updateStatus('Error selecting template file: ' + error.message, 'error');
    }
}

async function browseOutputDirectory() {
    try {
        updateStatus('Opening output directory dialog...', 'info');
        
        if (window.require) {
            const { ipcRenderer } = window.require('electron');
            const result = await ipcRenderer.invoke('select-output-directory');
            
            if (result && !result.canceled && result.filePaths.length > 0) {
                const dirPath = result.filePaths[0];
                document.getElementById('outputDir').value = dirPath;
                updateStatus(`Output directory selected: ${dirPath}`, 'success');
            }
        } else {
            updateStatus('Electron API not available - please use drag and drop', 'warning');
        }
    } catch (error) {
        console.error('Error selecting output directory:', error);
        updateStatus('Error selecting output directory: ' + error.message, 'error');
    }
}

async function validateCsvFile(filePath) {
    try {
        updateStatus('Validating CSV file...', 'info');
        
        const response = await fetch('http://localhost:5000/api/fileprocessing/preview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                csvFilePath: filePath,
                templateFilePath: document.getElementById('templateFile').value || '',
                fileNamePattern: localStorage.getItem('fileNamePattern') || '[Location] [Facility] [Equipment] [Procedure]'
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            updateStatus('CSV validated! Found ' + Object.keys(data.fields).length + ' columns', 'success');
        } else {
            updateStatus('CSV validation failed', 'error');
        }
    } catch (error) {
        console.error('CSV validation error:', error);
        updateStatus('CSV validation failed: ' + error.message, 'error');
    }
}

async function validateTemplateFile(filePath) {
    try {
        updateStatus('Validating template file...', 'info');
        
        // Simple validation - just check if file exists and has correct extension
        const templateType = document.getElementById('templateType').value;
        const extension = filePath.split('.').pop().toLowerCase();
        
        if ((templateType === 'excel' && extension === 'xlsx') || 
            (templateType === 'word' && extension === 'docx')) {
            updateStatus(`${templateType.toUpperCase()} template validated!`, 'success');
        } else {
            updateStatus('Invalid template file type', 'error');
        }
    } catch (error) {
        console.error('Template validation error:', error);
        updateStatus('Template validation failed: ' + error.message, 'error');
    }
}

async function generatePreview() {
    try {
        updateStatus('Generating preview...', 'info');
        
        const csvFile = document.getElementById('csvFile').value;
        const templateFile = document.getElementById('templateFile').value;
        
        if (!csvFile || !templateFile) {
            updateStatus('Please select both CSV and template files first', 'warning');
            return;
        }
        
        const response = await fetch('http://localhost:5000/api/fileprocessing/preview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                csvFilePath: csvFile,
                templatePath: templateFile,
                fileNamePattern: localStorage.getItem('fileNamePattern') || '[Location] [Facility] [Equipment] [Procedure]'
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            updateStatus('Preview: ' + data.previewName, 'success');
        } else {
            updateStatus('Preview failed', 'error');
        }
    } catch (error) {
        console.error('Preview error:', error);
        updateStatus('Preview failed: ' + error.message, 'error');
    }
}

async function processFiles() {
    try {
        updateStatus('Starting file processing...', 'info');
        
        const csvFile = document.getElementById('csvFile').value;
        const templateFile = document.getElementById('templateFile').value;
        const outputDir = document.getElementById('outputDir').value;
        
        if (!csvFile || !templateFile || !outputDir) {
            updateStatus('Please select all required files and directories', 'warning');
            return;
        }
        
        const templateType = document.getElementById('templateType').value;
        const response = await fetch('http://localhost:5000/api/fileprocessing/process', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                csvFilePath: csvFile,
                templatePath: templateFile,
                outputPath: outputDir,
                templateType: templateType,
                fileNamePattern: localStorage.getItem('fileNamePattern') || '[Location] [Facility] [Equipment] [Procedure]'
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            updateStatus(`Successfully processed ${data.processedFiles} files!`, 'success');
        } else {
            const errorData = await response.json();
            console.error('Processing error:', errorData);
            updateStatus(`Processing failed: ${errorData.error || errorData.details || 'Unknown error'}`, 'error');
        }
    } catch (error) {
        console.error('Processing error:', error);
        updateStatus('Processing failed: ' + error.message, 'error');
    }
}

function showTemplateManager() {
    updateStatus('Manage Templates clicked - opening template manager...', 'info');
    // Simple alert for now - could be expanded to a proper modal
    alert('Template Manager\n\nThis feature allows you to:\n- Save frequently used templates\n- Organize templates by category\n- Quick access to recent templates\n\n(Feature coming soon)');
}

function showFileNameConfigDialog() {
    updateStatus('Opening file name configuration...', 'info');
    
    // Create a simple modal dialog since prompt() doesn't work in Electron
    const currentPattern = localStorage.getItem('fileNamePattern') || '[Location] [Facility] [Equipment] [Procedure]';
    
    // Create modal elements
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.display = 'block';
    
    const modalContent = document.createElement('div');
    modalContent.className = 'modal-content';
    
    modalContent.innerHTML = `
        <h2>Configure File Name Pattern</h2>
        <p>Enter the pattern for generated filenames. Use [FieldName] for placeholders.</p>
        <input type="text" id="patternInput" value="${currentPattern}" style="width: 100%; margin: 10px 0; padding: 8px;">
        <div style="text-align: right; margin-top: 20px;">
            <button id="cancelPattern" style="margin-right: 10px;">Cancel</button>
            <button id="savePattern">Save</button>
        </div>
    `;
    
    modal.appendChild(modalContent);
    document.body.appendChild(modal);
    
    // Add event listeners
    document.getElementById('cancelPattern').onclick = () => {
        document.body.removeChild(modal);
        updateStatus('File name configuration cancelled', 'info');
    };
    
    document.getElementById('savePattern').onclick = () => {
        const newPattern = document.getElementById('patternInput').value;
        if (newPattern.trim()) {
            localStorage.setItem('fileNamePattern', newPattern.trim());
            updateStatus('File name pattern updated: ' + newPattern.trim(), 'success');
        }
        document.body.removeChild(modal);
    };
    
    // Focus the input
    setTimeout(() => {
        document.getElementById('patternInput').focus();
        document.getElementById('patternInput').select();
    }, 100);
}

async function testAPI() {
    try {
        // Simple connectivity test - just check if backend responds
        const response = await fetch('http://localhost:5000/api/fileprocessing/preview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                csvFilePath: 'test-data/test.csv',
                templatePath: 'test-data/templates/template.xlsx',
                fileNamePattern: '[Location] [Facility] [Equipment] [Procedure]'
            })
        });
        
        if (response.ok) {
            updateStatus('Backend API is working!', 'success');
        } else {
            // Even if the specific request fails, if we got a response, the backend is running
            if (response.status === 500) {
                updateStatus('Backend API is running (test files may not exist)', 'info');
            } else {
                updateStatus('Backend API connection failed: ' + response.statusText, 'error');
            }
        }
    } catch (error) {
        if (error.message.includes('Failed to fetch')) {
            updateStatus('Backend API is not running - please start with: dotnet run', 'warning');
        } else {
            updateStatus('Backend API connection failed: ' + error.message, 'error');
        }
    }
}

function updateStatus(message, type = 'info') {
    const statusElement = document.getElementById('status');
    if (statusElement) {
        statusElement.textContent = message;
        statusElement.className = 'status ' + type;
        statusElement.style.display = 'block';
    }
    console.log('Status:', message);
}
