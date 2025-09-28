// Simple app.js for Document Template Processor
console.log('App loaded');

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, setting up handlers');
    
    // Load and apply settings
    const settings = loadSettings();
    const templateTypeElement = document.getElementById('templateType');
    if (templateTypeElement) {
        templateTypeElement.value = settings.templateType;
    }
    
    // fileNamePattern is handled in the configure filename dialog, not a direct input
    
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
    document.getElementById('settings')?.addEventListener('click', showSettingsDialog);
    
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
                
                // Security: Validate file type and path
                if (!filePath.toLowerCase().endsWith('.csv')) {
                    updateStatus('Please select a valid CSV file.', 'error');
                    return;
                }
                
                if (filePath.includes('..') || filePath.includes('~')) {
                    updateStatus('Invalid file path detected.', 'error');
                    return;
                }
                
                document.getElementById('csvFile').value = filePath;
                updateStatus(`CSV file selected: ${filePath}`, 'success');
                
                // Remember directory
                const settings = loadSettings();
                settings.lastCsvDirectory = filePath.substring(0, filePath.lastIndexOf('\\'));
                saveSettings(settings);
                
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
                
                // Security: Validate file type and path
                const allowedExtensions = templateType === 'excel' ? ['.xlsx', '.xls'] : ['.docx', '.doc'];
                const fileExt = filePath.toLowerCase().substring(filePath.lastIndexOf('.'));
                if (!allowedExtensions.includes(fileExt)) {
                    updateStatus(`Please select a valid ${templateType} file.`, 'error');
                    return;
                }
                
                if (filePath.includes('..') || filePath.includes('~')) {
                    updateStatus('Invalid file path detected.', 'error');
                    return;
                }
                
                document.getElementById('templateFile').value = filePath;
                updateStatus(`${templateType.toUpperCase()} template selected: ${filePath}`, 'success');
                
                // Remember directory
                const settings = loadSettings();
                settings.lastTemplateDirectory = filePath.substring(0, filePath.lastIndexOf('\\'));
                saveSettings(settings);
                
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
                
                // Remember directory
                const settings = loadSettings();
                settings.lastOutputDirectory = dirPath;
                saveSettings(settings);
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
                templatePath: document.getElementById('templateFile').value || '',
                fileNamePattern: loadSettings().fileNamePattern
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
        
        // Show loading indicator
        const statusElement = document.getElementById('status');
        if (statusElement) {
            statusElement.style.opacity = '0.7';
        }
        
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
                fileNamePattern: loadSettings().fileNamePattern
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            updateStatus('Preview: ' + data.previewName, 'success');
            
            // Reset loading indicator
            const statusElement = document.getElementById('status');
            if (statusElement) {
                statusElement.style.opacity = '1';
            }
            
            // Show detailed validation UI
            showTemplateValidationUI(data, csvFile, templateFile);
        } else {
            updateStatus('Preview failed', 'error');
            
            // Reset loading indicator
            const statusElement = document.getElementById('status');
            if (statusElement) {
                statusElement.style.opacity = '1';
            }
        }
    } catch (error) {
        console.error('Preview error:', error);
        updateStatus('Preview failed: ' + error.message, 'error');
        
        // Reset loading indicator
        const statusElement = document.getElementById('status');
        if (statusElement) {
            statusElement.style.opacity = '1';
        }
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
                fileNamePattern: loadSettings().fileNamePattern
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
    updateStatus('Opening template manager...', 'info');
    
    // Get saved templates from localStorage
    const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
    
    // Create modal elements
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.display = 'block';
    
    const modalContent = document.createElement('div');
    modalContent.className = 'modal-content';
    modalContent.style.maxWidth = '900px';
    modalContent.style.width = '95%';
    modalContent.style.maxHeight = '80vh';
    modalContent.style.overflow = 'hidden';
    modalContent.style.display = 'flex';
    modalContent.style.flexDirection = 'column';
    
    modalContent.innerHTML = `
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; padding-bottom: 15px; border-bottom: 1px solid #e2e8f0;">
            <h2 style="margin: 0;">Template Manager</h2>
            <button id="addTemplateBtn" style="padding: 8px 16px; background: #2563eb; color: white; border: none; border-radius: 6px; cursor: pointer;">
                + Add Current Template
            </button>
        </div>
        
        <div style="flex: 1; overflow-y: auto; margin-bottom: 20px;">
            ${savedTemplates.length > 0 ? `
                <div style="display: flex; gap: 20px; height: 100%;">
                    <!-- Template List Table -->
                    <div style="flex: 1; min-width: 0;">
                        <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;">
                            <div style="background: #f1f5f9; padding: 12px; border-bottom: 1px solid #e2e8f0; font-weight: 600; color: #475569;">
                                Saved Templates (${savedTemplates.length})
                            </div>
                            <div style="max-height: 500px; overflow-y: auto; border: 1px solid #e2e8f0; border-top: none;">
                                <table style="width: 100%; border-collapse: collapse;">
                                    <thead style="background: #f8fafc; position: sticky; top: 0; z-index: 10;">
                                        <tr>
                                            <th style="padding: 8px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Name</th>
                                            <th style="padding: 8px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Type</th>
                                            <th style="padding: 8px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Category</th>
                                            <th style="padding: 8px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Date</th>
                                            <th style="padding: 8px 12px; text-align: center; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${savedTemplates.map((template, index) => `
                                            <tr class="template-row" data-index="${index}" style="border-bottom: 1px solid #f1f5f9; cursor: pointer; transition: background-color 0.2s;">
                                                <td style="padding: 10px 12px; font-size: 14px; font-weight: 500; color: #1e293b;">
                                                    <div style="display: flex; align-items: center; gap: 8px;">
                                                        <span>${template.name}</span>
                                                    </div>
                                                </td>
                                                <td style="padding: 10px 12px;">
                                                    <span class="template-type-badge ${template.type.toLowerCase()}" style="font-size: 11px;">${template.type}</span>
                                                </td>
                                                <td style="padding: 10px 12px; font-size: 13px; color: #64748b;">${template.category || 'Uncategorized'}</td>
                                                <td style="padding: 10px 12px; font-size: 12px; color: #94a3b8;">${new Date(template.dateAdded).toLocaleDateString()}</td>
                                                <td style="padding: 10px 12px; text-align: center;">
                                                    <div style="display: flex; gap: 4px; justify-content: center;">
                                                        <button class="action-btn load-template" data-index="${index}" style="padding: 4px 8px; font-size: 11px;">Load</button>
                                                        <button class="action-btn delete-template" data-index="${index}" style="padding: 4px 8px; font-size: 11px;">Delete</button>
                                                    </div>
                                                </td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Preview Panel -->
                    <div id="previewPanel" style="width: 300px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; display: none;">
                        <h3 style="margin: 0 0 12px 0; font-size: 16px; color: #1e293b;">Template Preview</h3>
                        <div id="previewContent">
                            <p style="color: #64748b; font-style: italic;">Select a template to see details</p>
                        </div>
                    </div>
                </div>
            ` : `
                <div style="text-align: center; padding: 40px; color: #64748b;">
                    <p>No templates saved yet.</p>
                    <p>Add your first template using the "Add Current Template" button above.</p>
                </div>
            `}
        </div>
        
        <div style="text-align: right; padding-top: 15px; border-top: 1px solid #e2e8f0;">
            <button id="closeTemplateManager" style="padding: 10px 20px;">Close</button>
        </div>
    `;
    
    modal.appendChild(modalContent);
    document.body.appendChild(modal);
    
    // Add styles for template manager
    const style = document.createElement('style');
    style.textContent = `
        .template-row:hover {
            background-color: #f8fafc !important;
        }
        .template-row.selected {
            background-color: #dbeafe !important;
            border-left: 3px solid #2563eb;
        }
        .template-type-badge {
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 12px;
            font-weight: 500;
            text-transform: uppercase;
        }
        .template-type-badge.excel {
            background: #dcfce7;
            color: #166534;
        }
        .template-type-badge.word {
            background: #dbeafe;
            color: #1e40af;
        }
        .template-info {
            margin-bottom: 10px;
        }
        .template-path {
            margin: 0 0 4px 0;
            font-size: 12px;
            color: #64748b;
            word-break: break-all;
            line-height: 1.2;
        }
        .template-category {
            margin: 0 0 2px 0;
            font-size: 11px;
            color: #94a3b8;
            font-weight: 500;
        }
        .template-date {
            margin: 0;
            font-size: 11px;
            color: #94a3b8;
        }
        .template-actions {
            display: flex;
            gap: 8px;
        }
        .template-actions .action-btn {
            padding: 6px 12px;
            border: 1px solid #e2e8f0;
            border-radius: 4px;
            background: #f8fafc;
            cursor: pointer;
            font-size: 12px;
            transition: all 0.2s;
        }
        .template-actions .action-btn:hover {
            background: #e2e8f0;
        }
        .template-actions .load-template:hover {
            background: #2563eb;
            color: white;
            border-color: #2563eb;
        }
        .template-actions .delete-template:hover {
            background: #dc2626;
            color: white;
            border-color: #dc2626;
        }
    `;
    document.head.appendChild(style);
    
    // Event listeners
    document.getElementById('addTemplateBtn').onclick = () => {
        addCurrentTemplate();
    };
    
    document.getElementById('closeTemplateManager').onclick = () => {
        // Remove all modals and styles
        const modals = document.querySelectorAll('.modal');
        modals.forEach(modal => modal.remove());
        
        const styles = document.querySelectorAll('style');
        styles.forEach(style => {
            if (style.textContent.includes('template-card') || style.textContent.includes('placeholder-btn')) {
                style.remove();
            }
        });
        
        updateStatus('Template manager closed', 'info');
    };
    
    // Template row click handlers
    document.querySelectorAll('.template-row').forEach(row => {
        row.onclick = (e) => {
            // Don't trigger if clicking on action buttons
            if (e.target.classList.contains('action-btn')) return;
            
            const index = parseInt(row.dataset.index);
            showTemplatePreview(index);
        };
    });
    
    // Action button handlers
    document.querySelectorAll('.load-template').forEach(btn => {
        btn.onclick = (e) => {
            e.stopPropagation();
            loadTemplate(parseInt(btn.dataset.index));
        };
    });
    
    document.querySelectorAll('.delete-template').forEach(btn => {
        btn.onclick = (e) => {
            e.stopPropagation();
            deleteTemplate(parseInt(btn.dataset.index));
        };
    });
}

function addCurrentTemplate() {
    const templateFile = document.getElementById('templateFile').value;
    const templateType = document.getElementById('templateType').value;
    
    if (!templateFile) {
        updateStatus('Please select a template file first', 'warning');
        return;
    }
    
    // Create add template modal
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.display = 'block';
    
    const modalContent = document.createElement('div');
    modalContent.className = 'modal-content';
    modalContent.style.maxWidth = '500px';
    
    modalContent.innerHTML = `
        <h2>Add Template</h2>
        <p>Save the current template for quick access later.</p>
        
        <div style="margin: 20px 0;">
            <label for="templateName" style="display: block; margin-bottom: 8px; font-weight: 500;">Template Name:</label>
            <input type="text" id="templateName" placeholder="Enter a name for this template" 
                   style="width: 100%; padding: 12px; border: 2px solid #e2e8f0; border-radius: 8px; font-size: 16px;">
        </div>
        
        <div style="margin: 20px 0;">
            <label for="templateCategory" style="display: block; margin-bottom: 8px; font-weight: 500;">Category (optional):</label>
            <input type="text" id="templateCategory" placeholder="e.g., Maintenance, Reports, etc." 
                   style="width: 100%; padding: 12px; border: 2px solid #e2e8f0; border-radius: 8px; font-size: 16px;">
        </div>
        
        <div style="margin: 20px 0; padding: 12px; background: #f8fafc; border-radius: 8px;">
            <p style="margin: 0 0 8px 0; font-weight: 500;">Template Details:</p>
            <p style="margin: 0; color: #64748b; font-size: 14px;">
                <strong>Type:</strong> ${templateType.toUpperCase()}<br>
                <strong>File:</strong> ${templateFile.split('\\').pop()}
            </p>
        </div>
        
        <div style="text-align: right; margin-top: 30px;">
            <button id="cancelAddTemplate" style="margin-right: 10px; padding: 10px 20px;">Cancel</button>
            <button id="saveTemplate" style="padding: 10px 20px; background: #2563eb; color: white; border: none; border-radius: 6px;">Save Template</button>
        </div>
    `;
    
    modal.appendChild(modalContent);
    document.body.appendChild(modal);
    
    // Event listeners
    document.getElementById('cancelAddTemplate').onclick = () => {
        document.body.removeChild(modal);
    };
    
    document.getElementById('saveTemplate').onclick = () => {
        const name = document.getElementById('templateName').value.trim();
        const category = document.getElementById('templateCategory').value.trim();
        
        if (!name) {
            updateStatus('Please enter a template name', 'warning');
            return;
        }
        
        // Save template
        const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
        const newTemplate = {
            name: name,
            category: category || 'Uncategorized',
            type: templateType,
            path: templateFile,
            dateAdded: new Date().toISOString()
        };
        
        savedTemplates.push(newTemplate);
        localStorage.setItem('savedTemplates', JSON.stringify(savedTemplates));
        
        document.body.removeChild(modal);
        updateStatus(`Template "${name}" saved successfully`, 'success');
        
        // Refresh the template manager
        showTemplateManager();
    };
    
    // Focus the name input
    setTimeout(() => {
        document.getElementById('templateName').focus();
    }, 100);
}

function showTemplatePreview(index) {
    const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
    const template = savedTemplates[index];
    
    if (!template) {
        updateStatus('Template not found', 'error');
        return;
    }
    
    // Remove previous selection
    document.querySelectorAll('.template-row').forEach(row => {
        row.classList.remove('selected');
    });
    
    // Highlight selected row
    const selectedRow = document.querySelector(`.template-row[data-index="${index}"]`);
    if (selectedRow) {
        selectedRow.classList.add('selected');
    }
    
    // Show preview panel
    const previewPanel = document.getElementById('previewPanel');
    const previewContent = document.getElementById('previewContent');
    
    if (previewPanel && previewContent) {
        previewPanel.style.display = 'block';
        
        const fileName = template.path.split('\\').pop() || template.path.split('/').pop();
        const fileSize = 'Unknown size'; // Could be enhanced to get actual file size
        
        previewContent.innerHTML = `
            <div style="margin-bottom: 16px;">
                <h4 style="margin: 0 0 8px 0; color: #1e293b; font-size: 18px;">${template.name}</h4>
                <div class="template-type-badge ${template.type.toLowerCase()}" style="display: inline-block; margin-bottom: 8px;">${template.type}</div>
            </div>
            
            <div style="margin-bottom: 12px;">
                <strong style="color: #374151; font-size: 12px;">File:</strong><br>
                <span style="color: #64748b; font-size: 13px; word-break: break-all;">${fileName}</span>
            </div>
            
            <div style="margin-bottom: 12px;">
                <strong style="color: #374151; font-size: 12px;">Category:</strong><br>
                <span style="color: #64748b; font-size: 13px;">${template.category || 'Uncategorized'}</span>
            </div>
            
            <div style="margin-bottom: 12px;">
                <strong style="color: #374151; font-size: 12px;">Added:</strong><br>
                <span style="color: #64748b; font-size: 13px;">${new Date(template.dateAdded).toLocaleString()}</span>
            </div>
            
            <div style="margin-bottom: 16px;">
                <strong style="color: #374151; font-size: 12px;">Full Path:</strong><br>
                <span style="color: #64748b; font-size: 11px; word-break: break-all; font-family: monospace;">${template.path}</span>
            </div>
            
            <div style="border-top: 1px solid #e2e8f0; padding-top: 12px;">
                <button onclick="loadTemplate(${index})" style="width: 100%; padding: 8px 12px; background: #2563eb; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 14px; margin-bottom: 8px;">
                    Load This Template
                </button>
                <button onclick="deleteTemplate(${index})" style="width: 100%; padding: 8px 12px; background: #dc2626; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 14px;">
                    Delete Template
                </button>
            </div>
        `;
    }
}

function loadTemplate(index) {
    const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
    const template = savedTemplates[index];
    
    if (!template) {
        updateStatus('Template not found', 'error');
        return;
    }
    
    // Set the template file and type
    document.getElementById('templateFile').value = template.path;
    document.getElementById('templateType').value = template.type.toLowerCase();
    
    // Close all modals and clean up styles
    const modals = document.querySelectorAll('.modal');
    modals.forEach(modal => modal.remove());
    
    const styles = document.querySelectorAll('style');
    styles.forEach(style => {
        if (style.textContent.includes('template-card') || style.textContent.includes('placeholder-btn')) {
            style.remove();
        }
    });
    
    updateStatus(`Template "${template.name}" loaded successfully`, 'success');
}

function deleteTemplate(index) {
    const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
    const template = savedTemplates[index];
    
    if (!template) {
        updateStatus('Template not found', 'error');
        return;
    }
    
    if (confirm(`Are you sure you want to delete the template "${template.name}"?`)) {
        savedTemplates.splice(index, 1);
        localStorage.setItem('savedTemplates', JSON.stringify(savedTemplates));
        updateStatus(`Template "${template.name}" deleted`, 'success');
        
        // Refresh the template manager
        showTemplateManager();
    }
}

async function showFileNameConfigDialog() {
    updateStatus('Opening file name configuration...', 'info');
    
    const currentPattern = loadSettings().fileNamePattern;
    
    // Get available placeholders from CSV
    let availablePlaceholders = [];
    const csvFile = document.getElementById('csvFile').value;
    
    if (csvFile) {
        try {
            const response = await fetch('http://localhost:5000/api/fileprocessing/preview', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    csvFilePath: csvFile,
                    templatePath: document.getElementById('templateFile').value || '',
                    fileNamePattern: currentPattern
                })
            });
            
            if (response.ok) {
                const data = await response.json();
                availablePlaceholders = Object.keys(data.fields || {});
            }
        } catch (error) {
            console.log('Could not load CSV columns:', error);
        }
    }
    
    // Create modal elements
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.display = 'block';
    
    const modalContent = document.createElement('div');
    modalContent.className = 'modal-content';
    modalContent.style.maxWidth = '700px';
    modalContent.style.width = '90%';
    
    modalContent.innerHTML = `
        <h2>Configure File Name Pattern</h2>
        <p>Build your filename pattern using the available placeholders below. Click on placeholders to insert them.</p>
        
        <div style="margin: 20px 0;">
            <label for="patternInput" style="display: block; margin-bottom: 8px; font-weight: 500;">Filename Pattern:</label>
            <input type="text" id="patternInput" value="${currentPattern}" 
                   style="width: 100%; padding: 12px; border: 2px solid #e2e8f0; border-radius: 8px; font-size: 16px; font-family: monospace;">
        </div>
        
        <div style="margin: 20px 0;">
            <label style="display: block; margin-bottom: 8px; font-weight: 500;">Available Placeholders:</label>
            <div id="placeholderButtons" style="display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 10px;">
                ${availablePlaceholders.length > 0 ? 
                    availablePlaceholders.map(p => `<button class="placeholder-btn" data-placeholder="[${p}]">[${p}]</button>`).join('') :
                    '<p style="color: #64748b; font-style: italic;">No CSV file selected. Please select a CSV file first to see available placeholders.</p>'
                }
            </div>
        </div>
        
        <div style="margin: 20px 0;">
            <label style="display: block; margin-bottom: 8px; font-weight: 500;">Preview:</label>
            <div id="filenamePreview" style="padding: 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; font-family: monospace; color: #1e293b;">
                ${currentPattern}
            </div>
        </div>
        
        <div style="margin: 20px 0;">
            <div style="display: flex; gap: 10px; flex-wrap: wrap;">
                <button id="addSpace" class="action-btn">Add Space</button>
                <button id="addDash" class="action-btn">Add Dash (-)</button>
                <button id="addUnderscore" class="action-btn">Add Underscore (_)</button>
                <button id="clearPattern" class="action-btn" style="background: #dc2626;">Clear</button>
            </div>
        </div>
        
        <div style="text-align: right; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e2e8f0;">
            <button id="cancelPattern" style="margin-right: 10px; padding: 10px 20px;">Cancel</button>
            <button id="savePattern" style="padding: 10px 20px; background: #2563eb; color: white; border: none; border-radius: 6px;">Save Pattern</button>
        </div>
    `;
    
    modal.appendChild(modalContent);
    document.body.appendChild(modal);
    
    // Add styles for the new elements
    const style = document.createElement('style');
    style.textContent = `
        .placeholder-btn {
            padding: 8px 12px;
            background: #e2e8f0;
            border: 1px solid #cbd5e1;
            border-radius: 6px;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s;
        }
        .placeholder-btn:hover {
            background: #2563eb;
            color: white;
            border-color: #2563eb;
        }
        .action-btn {
            padding: 8px 16px;
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 6px;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s;
        }
        .action-btn:hover {
            background: #e2e8f0;
        }
    `;
    document.head.appendChild(style);
    
    // Get references to elements
    const patternInput = document.getElementById('patternInput');
    const preview = document.getElementById('filenamePreview');
    const placeholderButtons = document.querySelectorAll('.placeholder-btn');
    const actionButtons = {
        addSpace: document.getElementById('addSpace'),
        addDash: document.getElementById('addDash'),
        addUnderscore: document.getElementById('addUnderscore'),
        clearPattern: document.getElementById('clearPattern')
    };
    
    // Update preview function
    function updatePreview() {
        const pattern = patternInput.value;
        preview.textContent = pattern || 'Enter a pattern...';
        preview.style.color = pattern ? '#1e293b' : '#94a3b8';
    }
    
    // Add placeholder to pattern
    function addPlaceholder(placeholder) {
        const currentValue = patternInput.value;
        const cursorPos = patternInput.selectionStart;
        const newValue = currentValue.slice(0, cursorPos) + placeholder + currentValue.slice(cursorPos);
        patternInput.value = newValue;
        patternInput.focus();
        patternInput.setSelectionRange(cursorPos + placeholder.length, cursorPos + placeholder.length);
        updatePreview();
    }
    
    // Add text to pattern
    function addText(text) {
        const currentValue = patternInput.value;
        const cursorPos = patternInput.selectionStart;
        const newValue = currentValue.slice(0, cursorPos) + text + currentValue.slice(cursorPos);
        patternInput.value = newValue;
        patternInput.focus();
        patternInput.setSelectionRange(cursorPos + text.length, cursorPos + text.length);
        updatePreview();
    }
    
    // Event listeners
    patternInput.addEventListener('input', updatePreview);
    patternInput.addEventListener('keyup', updatePreview);
    
    placeholderButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            addPlaceholder(btn.dataset.placeholder);
        });
    });
    
    actionButtons.addSpace.addEventListener('click', () => addText(' '));
    actionButtons.addDash.addEventListener('click', () => addText('-'));
    actionButtons.addUnderscore.addEventListener('click', () => addText('_'));
    actionButtons.clearPattern.addEventListener('click', () => {
        patternInput.value = '';
        updatePreview();
        patternInput.focus();
    });
    
    document.getElementById('cancelPattern').onclick = () => {
        document.body.removeChild(modal);
        document.head.removeChild(style);
        updateStatus('File name configuration cancelled', 'info');
    };
    
    document.getElementById('savePattern').onclick = () => {
        const newPattern = patternInput.value.trim();
        if (newPattern) {
            updateSetting('fileNamePattern', newPattern);
            updateStatus('File name pattern updated: ' + newPattern, 'success');
        }
        document.body.removeChild(modal);
        document.head.removeChild(style);
    };
    
    // Focus the input and update preview
    setTimeout(() => {
        patternInput.focus();
        patternInput.select();
        updatePreview();
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

// Enhanced Settings Management System
function loadSettings() {
    const defaultSettings = {
        fileNamePattern: '[Location] [Facility] [Equipment] [Procedure]',
        lastUsedDirectory: '',
        lastCsvDirectory: '',
        lastTemplateDirectory: '',
        lastOutputDirectory: '',
        templateType: 'excel',
        placeholderPattern: 'single', // 'single', 'double', 'curly', 'percent', 'custom'
        customPlaceholderStart: '[',
        customPlaceholderEnd: ']',
        windowWidth: 800,
        windowHeight: 600,
        showAdvancedOptions: false,
        autoSaveTemplates: true,
        rememberMappings: true,
        columnMappings: {} // CSV column to placeholder mappings
    };
    
    const savedSettings = JSON.parse(localStorage.getItem('appSettings') || '{}');
    return { ...defaultSettings, ...savedSettings };
}

function saveSettings(settings) {
    localStorage.setItem('appSettings', JSON.stringify(settings));
}

function updateSetting(key, value) {
    const settings = loadSettings();
    settings[key] = value;
    saveSettings(settings);
    return settings;
}

function getPlaceholderPattern(settings = null) {
    if (!settings) settings = loadSettings();
    
    switch (settings.placeholderPattern) {
        case 'single':
            return { start: '[', end: ']' };
        case 'double':
            return { start: '[[', end: ']]' };
        case 'curly':
            return { start: '{', end: '}' };
        case 'percent':
            return { start: '%', end: '%' };
        case 'custom':
            return { start: settings.customPlaceholderStart, end: settings.customPlaceholderEnd };
        default:
            return { start: '[', end: ']' };
    }
}

function extractPlaceholders(text, pattern = null) {
    if (!pattern) pattern = getPlaceholderPattern();
    
    const escapedStart = escapeRegExp(pattern.start);
    const escapedEnd = escapeRegExp(pattern.end);
    
    // Create regex that handles nested delimiters intelligently
    const regex = new RegExp(`${escapedStart}([^${escapedEnd}]+?)${escapedEnd}`, 'g');
    
    const placeholders = [];
    let match;
    
    while ((match = regex.exec(text)) !== null) {
        const placeholderName = match[1].trim();
        
        // Skip empty placeholders
        if (!placeholderName) continue;
        
        // Skip if it's just whitespace or special characters
        if (!/^[a-zA-Z0-9_-]+$/.test(placeholderName)) continue;
        
        placeholders.push({
            full: match[0],
            name: placeholderName,
            start: match.index,
            end: match.index + match[0].length,
            pattern: { start: pattern.start, end: pattern.end }
        });
    }
    
    return placeholders;
}

function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// Advanced placeholder detection with multiple pattern support
function detectAllPlaceholders(text, settings = null) {
    if (!settings) settings = loadSettings();
    
    const patterns = getAllPlaceholderPatterns(settings);
    const allPlaceholders = [];
    const patternResults = {};
    
    // Detect placeholders with each pattern
    patterns.forEach(pattern => {
        const placeholders = extractPlaceholders(text, pattern);
        patternResults[pattern.name] = placeholders;
        allPlaceholders.push(...placeholders.map(p => ({
            ...p,
            detectedBy: pattern.name,
            confidence: calculateConfidence(p, pattern, text)
        })));
    });
    
    // Remove duplicates and merge results
    const uniquePlaceholders = mergeDuplicatePlaceholders(allPlaceholders);
    
    return {
        placeholders: uniquePlaceholders,
        patternResults,
        totalFound: uniquePlaceholders.length,
        patternsUsed: patterns.map(p => p.name),
        suggestions: generatePlaceholderSuggestions(uniquePlaceholders, text)
    };
}

function getAllPlaceholderPatterns(settings) {
    const patterns = [];
    
    // Add the user's preferred pattern first
    const preferredPattern = getPlaceholderPattern(settings);
    patterns.push({
        name: 'user_preferred',
        ...preferredPattern,
        priority: 1
    });
    
    // Add common patterns
    const commonPatterns = [
        { name: 'single_brackets', start: '[', end: ']', priority: 2 },
        { name: 'double_brackets', start: '[[', end: ']]', priority: 3 },
        { name: 'curly_braces', start: '{', end: '}', priority: 4 },
        { name: 'percent_signs', start: '%', end: '%', priority: 5 },
        { name: 'parentheses', start: '(', end: ')', priority: 6 },
        { name: 'angle_brackets', start: '<', end: '>', priority: 7 }
    ];
    
    // Only add common patterns if they're different from the user's preferred pattern
    commonPatterns.forEach(pattern => {
        if (pattern.start !== preferredPattern.start || pattern.end !== preferredPattern.end) {
            patterns.push(pattern);
        }
    });
    
    return patterns;
}

function calculateConfidence(placeholder, pattern, text) {
    let confidence = 0.5; // Base confidence
    
    // Higher confidence for user's preferred pattern
    if (pattern.name === 'user_preferred') {
        confidence += 0.3;
    }
    
    // Higher confidence for longer, more descriptive names
    if (placeholder.name.length > 3) {
        confidence += 0.1;
    }
    
    // Higher confidence for common field names
    const commonFields = ['name', 'date', 'time', 'location', 'address', 'phone', 'email', 'id', 'number', 'code'];
    if (commonFields.some(field => placeholder.name.toLowerCase().includes(field))) {
        confidence += 0.1;
    }
    
    // Higher confidence if placeholder appears multiple times
    const occurrences = (text.match(new RegExp(escapeRegExp(placeholder.full), 'g')) || []).length;
    if (occurrences > 1) {
        confidence += 0.1;
    }
    
    // Higher confidence for patterns that are less likely to be false positives
    if (pattern.start === '[[' && pattern.end === ']]') {
        confidence += 0.1; // Double brackets are very specific
    }
    
    return Math.min(confidence, 1.0);
}

function mergeDuplicatePlaceholders(placeholders) {
    const unique = new Map();
    
    placeholders.forEach(placeholder => {
        const key = placeholder.name.toLowerCase();
        
        if (!unique.has(key)) {
            unique.set(key, placeholder);
        } else {
            const existing = unique.get(key);
            // Keep the one with higher confidence
            if (placeholder.confidence > existing.confidence) {
                unique.set(key, placeholder);
            }
        }
    });
    
    return Array.from(unique.values()).sort((a, b) => b.confidence - a.confidence);
}

function generatePlaceholderSuggestions(placeholders, text) {
    const suggestions = [];
    
    // Find potential placeholders that weren't detected
    const detectedNames = new Set(placeholders.map(p => p.name.toLowerCase()));
    
    // Look for common patterns in the text
    const commonPatterns = [
        /\b([A-Z][a-z]+[A-Z][a-z]+)\b/g, // CamelCase
        /\b([a-z]+_[a-z]+)\b/g, // snake_case
        /\b([a-z]+-[a-z]+)\b/g, // kebab-case
        /\b([A-Z]+_[A-Z]+)\b/g // CONSTANT_CASE
    ];
    
    commonPatterns.forEach(pattern => {
        let match;
        while ((match = pattern.exec(text)) !== null) {
            const name = match[1].toLowerCase();
            if (!detectedNames.has(name) && name.length > 2) {
                suggestions.push({
                    name: match[1],
                    type: 'pattern_match',
                    confidence: 0.3,
                    reason: 'Found in text but not as placeholder'
                });
            }
        }
    });
    
    return suggestions;
}

// Smart Suggestions System
function generateSmartSuggestions(placeholders, csvColumns, settings = null) {
    if (!settings) settings = loadSettings();
    
    const suggestions = {
        exactMatches: [],
        fuzzyMatches: [],
        semanticMatches: [],
        patternMatches: [],
        conflictWarnings: [],
        optimizationTips: []
    };
    
    // Generate different types of suggestions
    suggestions.exactMatches = findExactMatches(placeholders, csvColumns);
    suggestions.fuzzyMatches = findFuzzyMatches(placeholders, csvColumns);
    suggestions.semanticMatches = findSemanticMatches(placeholders, csvColumns);
    suggestions.patternMatches = findPatternMatches(placeholders, csvColumns);
    suggestions.conflictWarnings = findConflictWarnings(placeholders, csvColumns);
    suggestions.optimizationTips = generateOptimizationTips(placeholders, csvColumns);
    
    return suggestions;
}

function findExactMatches(placeholders, csvColumns) {
    const matches = [];
    
    placeholders.forEach(placeholder => {
        const exactMatch = csvColumns.find(col => 
            col.toLowerCase() === placeholder.name.toLowerCase()
        );
        
        if (exactMatch) {
            matches.push({
                placeholder: placeholder.name,
                column: exactMatch,
                confidence: 1.0,
                type: 'exact',
                reason: 'Exact name match'
            });
        }
    });
    
    return matches;
}

function findFuzzyMatches(placeholders, csvColumns) {
    const matches = [];
    
    // Limit processing for performance
    const maxPlaceholders = Math.min(placeholders.length, 20);
    const maxColumns = Math.min(csvColumns.length, 50);
    
    for (let i = 0; i < maxPlaceholders; i++) {
        const placeholder = placeholders[i];
        const fuzzyMatches = [];
        
        for (let j = 0; j < maxColumns; j++) {
            const col = csvColumns[j];
            const similarity = calculateSimilarity(col.toLowerCase(), placeholder.name.toLowerCase());
            
            if (similarity > 0.6) {
                fuzzyMatches.push({
                    column: col,
                    similarity: similarity
                });
            }
        }
        
        // Sort and take top 3
        fuzzyMatches
            .sort((a, b) => b.similarity - a.similarity)
            .slice(0, 3)
            .forEach(match => {
                matches.push({
                    placeholder: placeholder.name,
                    column: match.column,
                    confidence: match.similarity,
                    type: 'fuzzy',
                    reason: `Similar name (${Math.round(match.similarity * 100)}% match)`
                });
            });
    }
    
    return matches;
}

function findSemanticMatches(placeholders, csvColumns) {
    const matches = [];
    
    // Define semantic field mappings
    const semanticMappings = {
        // Location related
        'location': ['address', 'site', 'place', 'venue', 'building', 'facility'],
        'address': ['location', 'site', 'place', 'venue', 'building', 'facility'],
        'site': ['location', 'address', 'place', 'venue', 'building', 'facility'],
        
        // Time related
        'date': ['time', 'day', 'month', 'year', 'timestamp', 'created', 'updated'],
        'time': ['date', 'day', 'month', 'year', 'timestamp', 'created', 'updated'],
        
        // Person related
        'name': ['person', 'user', 'employee', 'staff', 'contact', 'responsible'],
        'person': ['name', 'user', 'employee', 'staff', 'contact', 'responsible'],
        'user': ['name', 'person', 'employee', 'staff', 'contact', 'responsible'],
        
        // Equipment related
        'equipment': ['device', 'machine', 'tool', 'apparatus', 'instrument', 'unit'],
        'device': ['equipment', 'machine', 'tool', 'apparatus', 'instrument', 'unit'],
        'machine': ['equipment', 'device', 'tool', 'apparatus', 'instrument', 'unit'],
        
        // ID related
        'id': ['number', 'code', 'identifier', 'key', 'reference', 'ref'],
        'number': ['id', 'code', 'identifier', 'key', 'reference', 'ref'],
        'code': ['id', 'number', 'identifier', 'key', 'reference', 'ref']
    };
    
    placeholders.forEach(placeholder => {
        const placeholderLower = placeholder.name.toLowerCase();
        
        // Find semantic matches
        Object.keys(semanticMappings).forEach(key => {
            if (placeholderLower.includes(key)) {
                const relatedTerms = semanticMappings[key];
                
                relatedTerms.forEach(term => {
                    const matchingColumns = csvColumns.filter(col => 
                        col.toLowerCase().includes(term)
                    );
                    
                    matchingColumns.forEach(column => {
                        matches.push({
                            placeholder: placeholder.name,
                            column: column,
                            confidence: 0.8,
                            type: 'semantic',
                            reason: `Semantic match: ${key} → ${term}`,
                            semanticKey: key,
                            semanticTerm: term
                        });
                    });
                });
            }
        });
    });
    
    return matches;
}

function findPatternMatches(placeholders, csvColumns) {
    const matches = [];
    
    placeholders.forEach(placeholder => {
        const placeholderLower = placeholder.name.toLowerCase();
        
        // Look for pattern-based matches
        const patterns = [
            // Abbreviation patterns
            { pattern: /^([a-z]+)(\d+)$/, type: 'abbreviation_number' },
            { pattern: /^([a-z]+)_([a-z]+)$/, type: 'snake_case' },
            { pattern: /^([a-z]+)-([a-z]+)$/, type: 'kebab_case' },
            { pattern: /^([A-Z][a-z]+)([A-Z][a-z]+)$/, type: 'camelCase' }
        ];
        
        patterns.forEach(({ pattern, type }) => {
            const match = placeholderLower.match(pattern);
            if (match) {
                // Find columns that match the pattern components
                const components = match.slice(1);
                
                csvColumns.forEach(column => {
                    const columnLower = column.toLowerCase();
                    let matchScore = 0;
                    
                    components.forEach(component => {
                        if (columnLower.includes(component)) {
                            matchScore += 1;
                        }
                    });
                    
                    if (matchScore > 0) {
                        matches.push({
                            placeholder: placeholder.name,
                            column: column,
                            confidence: matchScore / components.length,
                            type: 'pattern',
                            reason: `Pattern match: ${type}`,
                            patternType: type,
                            components: components
                        });
                    }
                });
            }
        });
    });
    
    return matches;
}

function findConflictWarnings(placeholders, csvColumns) {
    const warnings = [];
    
    // Check for potential conflicts
    placeholders.forEach(placeholder => {
        const similarPlaceholders = placeholders.filter(p => 
            p.name !== placeholder.name && 
            calculateSimilarity(p.name.toLowerCase(), placeholder.name.toLowerCase()) > 0.8
        );
        
        if (similarPlaceholders.length > 0) {
            warnings.push({
                type: 'similar_placeholders',
                placeholder: placeholder.name,
                similar: similarPlaceholders.map(p => p.name),
                message: `Similar placeholders found: ${similarPlaceholders.map(p => p.name).join(', ')}`
            });
        }
    });
    
    // Check for ambiguous column names
    csvColumns.forEach(column => {
        const similarColumns = csvColumns.filter(c => 
            c !== column && 
            calculateSimilarity(c.toLowerCase(), column.toLowerCase()) > 0.8
        );
        
        if (similarColumns.length > 0) {
            warnings.push({
                type: 'similar_columns',
                column: column,
                similar: similarColumns,
                message: `Similar columns found: ${similarColumns.join(', ')}`
            });
        }
    });
    
    return warnings;
}

function generateOptimizationTips(placeholders, csvColumns) {
    const tips = [];
    
    // Check for unused columns
    const usedColumns = new Set();
    placeholders.forEach(placeholder => {
        const exactMatch = csvColumns.find(col => 
            col.toLowerCase() === placeholder.name.toLowerCase()
        );
        if (exactMatch) {
            usedColumns.add(exactMatch);
        }
    });
    
    const unusedColumns = csvColumns.filter(col => !usedColumns.has(col));
    if (unusedColumns.length > 0) {
        tips.push({
            type: 'unused_columns',
            message: `${unusedColumns.length} CSV columns are not being used`,
            columns: unusedColumns,
            suggestion: 'Consider mapping these columns to placeholders or removing them'
        });
    }
    
    // Check for unmapped placeholders
    const mappedPlaceholders = new Set();
    placeholders.forEach(placeholder => {
        const exactMatch = csvColumns.find(col => 
            col.toLowerCase() === placeholder.name.toLowerCase()
        );
        if (exactMatch) {
            mappedPlaceholders.add(placeholder.name);
        }
    });
    
    const unmappedPlaceholders = placeholders.filter(p => !mappedPlaceholders.has(p.name));
    if (unmappedPlaceholders.length > 0) {
        tips.push({
            type: 'unmapped_placeholders',
            message: `${unmappedPlaceholders.length} placeholders are not mapped`,
            placeholders: unmappedPlaceholders.map(p => p.name),
            suggestion: 'Consider mapping these placeholders to CSV columns'
        });
    }
    
    // Check for naming conventions
    const hasConsistentNaming = checkNamingConsistency(placeholders, csvColumns);
    if (!hasConsistentNaming) {
        tips.push({
            type: 'naming_convention',
            message: 'Inconsistent naming conventions detected',
            suggestion: 'Consider standardizing naming conventions for better mapping'
        });
    }
    
    return tips;
}

function checkNamingConsistency(placeholders, csvColumns) {
    // Check if placeholders follow a consistent pattern
    const placeholderPatterns = placeholders.map(p => {
        if (/^[a-z]+_[a-z]+$/.test(p.name)) return 'snake_case';
        if (/^[a-z]+-[a-z]+$/.test(p.name)) return 'kebab_case';
        if (/^[A-Z][a-z]+[A-Z][a-z]+$/.test(p.name)) return 'camelCase';
        if (/^[A-Z]+_[A-Z]+$/.test(p.name)) return 'CONSTANT_CASE';
        return 'mixed';
    });
    
    const columnPatterns = csvColumns.map(c => {
        if (/^[a-z]+_[a-z]+$/.test(c)) return 'snake_case';
        if (/^[a-z]+-[a-z]+$/.test(c)) return 'kebab_case';
        if (/^[A-Z][a-z]+[A-Z][a-z]+$/.test(c)) return 'camelCase';
        if (/^[A-Z]+_[A-Z]+$/.test(c)) return 'CONSTANT_CASE';
        return 'mixed';
    });
    
    const uniquePlaceholderPatterns = new Set(placeholderPatterns);
    const uniqueColumnPatterns = new Set(columnPatterns);
    
    return uniquePlaceholderPatterns.size <= 2 && uniqueColumnPatterns.size <= 2;
}

// Settings UI Management
function showSettingsDialog() {
    const settings = loadSettings();
    
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 1000;
    `;
    
    modal.innerHTML = `
        <div style="background: white; border-radius: 8px; padding: 24px; max-width: 600px; width: 90%; max-height: 80vh; overflow-y: auto; box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);">
            <h2 style="margin: 0 0 20px 0; color: #1e293b;">Application Settings</h2>
            
            <div style="margin-bottom: 20px;">
                <h3 style="margin: 0 0 12px 0; color: #374151; font-size: 16px;">File Preferences</h3>
                
                <div style="margin-bottom: 12px;">
                    <label style="display: block; margin-bottom: 4px; font-weight: 500; color: #374151;">Default Template Type:</label>
                    <select id="settingsTemplateType" style="width: 100%; padding: 8px; border: 1px solid #d1d5db; border-radius: 4px;">
                        <option value="excel" ${settings.templateType === 'excel' ? 'selected' : ''}>Excel (.xlsx)</option>
                        <option value="word" ${settings.templateType === 'word' ? 'selected' : ''}>Word (.docx)</option>
                    </select>
                </div>
                
                <div style="margin-bottom: 12px;">
                    <label style="display: block; margin-bottom: 4px; font-weight: 500; color: #374151;">Default Filename Pattern:</label>
                    <input type="text" id="settingsFileNamePattern" value="${settings.fileNamePattern}" 
                           style="width: 100%; padding: 8px; border: 1px solid #d1d5db; border-radius: 4px; font-family: monospace;">
                    <small style="color: #6b7280; font-size: 12px;">Use [ColumnName] for CSV column placeholders</small>
                </div>
            </div>
            
            <div style="margin-bottom: 20px;">
                <h3 style="margin: 0 0 12px 0; color: #374151; font-size: 16px;">Placeholder Detection</h3>
                
                <div style="margin-bottom: 12px;">
                    <label style="display: block; margin-bottom: 4px; font-weight: 500; color: #374151;">Placeholder Style:</label>
                    <select id="settingsPlaceholderPattern" style="width: 100%; padding: 8px; border: 1px solid #d1d5db; border-radius: 4px;">
                        <option value="single" ${settings.placeholderPattern === 'single' ? 'selected' : ''}>Single brackets [Field]</option>
                        <option value="double" ${settings.placeholderPattern === 'double' ? 'selected' : ''}>Double brackets [[Field]]</option>
                        <option value="curly" ${settings.placeholderPattern === 'curly' ? 'selected' : ''}>Curly braces {Field}</option>
                        <option value="percent" ${settings.placeholderPattern === 'percent' ? 'selected' : ''}>Percent signs %Field%</option>
                        <option value="custom" ${settings.placeholderPattern === 'custom' ? 'selected' : ''}>Custom pattern</option>
                    </select>
                </div>
                
                <div id="customPatternSettings" style="display: ${settings.placeholderPattern === 'custom' ? 'block' : 'none'}; margin-top: 12px;">
                    <div style="display: flex; gap: 8px; align-items: center;">
                        <div style="flex: 1;">
                            <label style="display: block; margin-bottom: 4px; font-weight: 500; color: #374151;">Start marker:</label>
                            <input type="text" id="settingsCustomStart" value="${settings.customPlaceholderStart}" 
                                   style="width: 100%; padding: 8px; border: 1px solid #d1d5db; border-radius: 4px;">
                        </div>
                        <div style="flex: 1;">
                            <label style="display: block; margin-bottom: 4px; font-weight: 500; color: #374151;">End marker:</label>
                            <input type="text" id="settingsCustomEnd" value="${settings.customPlaceholderEnd}" 
                                   style="width: 100%; padding: 8px; border: 1px solid #d1d5db; border-radius: 4px;">
                        </div>
                    </div>
                    <div style="margin-top: 8px;">
                        <label style="display: block; margin-bottom: 4px; font-weight: 500; color: #374151;">Preview:</label>
                        <div id="patternPreview" style="padding: 8px; background: #f3f4f6; border-radius: 4px; font-family: monospace; font-size: 14px;">
                            ${settings.customPlaceholderStart}FieldName${settings.customPlaceholderEnd}
                        </div>
                    </div>
                </div>
            </div>
            
            <div style="margin-bottom: 20px;">
                <h3 style="margin: 0 0 12px 0; color: #374151; font-size: 16px;">Advanced Options</h3>
                
                <div style="margin-bottom: 12px;">
                    <label style="display: flex; align-items: center; gap: 8px;">
                        <input type="checkbox" id="settingsAutoSaveTemplates" ${settings.autoSaveTemplates ? 'checked' : ''}>
                        <span>Automatically save templates when added</span>
                    </label>
                </div>
                
                <div style="margin-bottom: 12px;">
                    <label style="display: flex; align-items: center; gap: 8px;">
                        <input type="checkbox" id="settingsRememberMappings" ${settings.rememberMappings ? 'checked' : ''}>
                        <span>Remember column mappings between sessions</span>
                    </label>
                </div>
                
                <div style="margin-bottom: 12px;">
                    <label style="display: flex; align-items: center; gap: 8px;">
                        <input type="checkbox" id="settingsShowAdvanced" ${settings.showAdvancedOptions ? 'checked' : ''}>
                        <span>Show advanced options in main interface</span>
                    </label>
                </div>
            </div>
            
            <div style="display: flex; gap: 12px; justify-content: flex-end; margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e7eb;">
                <button id="resetSettings" style="padding: 8px 16px; background: #6b7280; color: white; border: none; border-radius: 4px; cursor: pointer;">
                    Reset to Defaults
                </button>
                <button id="saveSettings" style="padding: 8px 16px; background: #2563eb; color: white; border: none; border-radius: 4px; cursor: pointer;">
                    Save Settings
                </button>
                <button id="closeSettings" style="padding: 8px 16px; background: #dc2626; color: white; border: none; border-radius: 4px; cursor: pointer;">
                    Cancel
                </button>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Event handlers
    document.getElementById('settingsPlaceholderPattern').onchange = function() {
        const customSettings = document.getElementById('customPatternSettings');
        customSettings.style.display = this.value === 'custom' ? 'block' : 'none';
        updatePatternPreview();
    };
    
    document.getElementById('settingsCustomStart').oninput = updatePatternPreview;
    document.getElementById('settingsCustomEnd').oninput = updatePatternPreview;
    
    function updatePatternPreview() {
        const start = document.getElementById('settingsCustomStart').value;
        const end = document.getElementById('settingsCustomEnd').value;
        document.getElementById('patternPreview').textContent = start + 'FieldName' + end;
    }
    
    document.getElementById('saveSettings').onclick = function() {
        const newSettings = {
            templateType: document.getElementById('settingsTemplateType').value,
            fileNamePattern: document.getElementById('settingsFileNamePattern').value,
            placeholderPattern: document.getElementById('settingsPlaceholderPattern').value,
            customPlaceholderStart: document.getElementById('settingsCustomStart').value,
            customPlaceholderEnd: document.getElementById('settingsCustomEnd').value,
            autoSaveTemplates: document.getElementById('settingsAutoSaveTemplates').checked,
            rememberMappings: document.getElementById('settingsRememberMappings').checked,
            showAdvancedOptions: document.getElementById('settingsShowAdvanced').checked
        };
        
        // Merge with existing settings
        const currentSettings = loadSettings();
        const updatedSettings = { ...currentSettings, ...newSettings };
        
        saveSettings(updatedSettings);
        
        // Update UI
        const templateTypeElement = document.getElementById('templateType');
        if (templateTypeElement) {
            templateTypeElement.value = updatedSettings.templateType;
        }
        // fileNamePattern is handled in the configure filename dialog, not a direct input
        
        modal.remove();
        updateStatus('Settings saved successfully', 'success');
    };
    
    document.getElementById('resetSettings').onclick = function() {
        if (confirm('Are you sure you want to reset all settings to defaults? This cannot be undone.')) {
            localStorage.removeItem('appSettings');
            modal.remove();
            showSettingsDialog(); // Reopen with defaults
        }
    };
    
    document.getElementById('closeSettings').onclick = function() {
        modal.remove();
    };
    
    // Close on background click
    modal.onclick = function(e) {
        if (e.target === modal) {
            modal.remove();
        }
    };
}

// Template Validation UI
function showTemplateValidationUI(previewData, csvFile, templateFile) {
    const settings = loadSettings();
    
    // Extract placeholders from both filename pattern and template content
    const filenamePlaceholders = detectAllPlaceholders(previewData.previewName || '', settings);
    const templatePlaceholders = extractPlaceholdersFromTemplateContent(previewData, settings);
    
    // Also detect placeholders from template content using all patterns
    // Create a simulated template content with the actual placeholders found
    const templateContentString = previewData.templatePlaceholders ? 
        previewData.templatePlaceholders.map(p => `[${p}]`).join(' ') : '';
    const templateContentPlaceholders = detectAllPlaceholders(templateContentString, settings);
    
    // Combine and deduplicate placeholders
    const allPlaceholders = [...filenamePlaceholders.placeholders, ...templatePlaceholders, ...templateContentPlaceholders.placeholders];
    const uniquePlaceholders = mergeDuplicatePlaceholders(allPlaceholders);
    
    // Merge pattern results from both filename and template content
    const mergedPatternResults = { ...filenamePlaceholders.patternResults };
    Object.keys(templateContentPlaceholders.patternResults || {}).forEach(pattern => {
        if (mergedPatternResults[pattern]) {
            mergedPatternResults[pattern] = [...mergedPatternResults[pattern], ...templateContentPlaceholders.patternResults[pattern]];
        } else {
            mergedPatternResults[pattern] = templateContentPlaceholders.patternResults[pattern];
        }
    });
    
    // Create detection results for display
    const detectionResults = {
        placeholders: uniquePlaceholders,
        patternResults: mergedPatternResults,
        totalFound: uniquePlaceholders.length,
        patternsUsed: [...new Set([...(filenamePlaceholders.patternsUsed || []), ...(templateContentPlaceholders.patternsUsed || [])])],
        suggestions: [...(filenamePlaceholders.suggestions || []), ...(templateContentPlaceholders.suggestions || [])]
    };
    
    // Get CSV columns
    const csvColumns = previewData.headers || Object.keys(previewData.fields || {});
    
    // Create mapping analysis
    const mappingAnalysis = analyzePlaceholderMapping(uniquePlaceholders, csvColumns);
    
    // Generate smart suggestions (only if we have placeholders and columns)
    const smartSuggestions = uniquePlaceholders.length > 0 && csvColumns.length > 0 ? 
        generateSmartSuggestions(uniquePlaceholders, csvColumns, settings) : 
        { exactMatches: [], fuzzyMatches: [], semanticMatches: [], patternMatches: [], conflictWarnings: [], optimizationTips: [] };
    
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        justify-content: center;
        align-items: flex-start;
        z-index: 1000;
        padding: 20px;
        overflow-y: auto;
    `;
    
    modal.innerHTML = `
        <div style="background: white; border-radius: 12px; padding: 32px; max-width: 1200px; width: 95%; max-height: 95vh; overflow-y: auto; box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15);">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
                <h2 style="margin: 0; color: #1e293b;">Template Validation & Preview</h2>
                <button id="closeValidation" style="background: #dc2626; color: white; border: none; border-radius: 4px; padding: 8px 12px; cursor: pointer;">✕ Close</button>
            </div>
            
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 24px;">
                <!-- Left Panel: Placeholder Analysis -->
                <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px;">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
                        <h3 style="margin: 0; color: #374151; font-size: 18px;">📋 Placeholder Analysis</h3>
                        <button id="toggleMappingMode" style="padding: 6px 12px; background: #6b7280; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 12px;">
                            📋 View Mode
                        </button>
                    </div>
                    
                    <div style="margin-bottom: 16px;">
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                            <span style="font-weight: 500; color: #374151;">Detection Method:</span>
                            <span style="padding: 4px 8px; background: #dbeafe; color: #1e40af; border-radius: 4px; font-size: 12px;">
                                🧠 Smart Detection
                            </span>
                        </div>
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                            <span style="font-weight: 500; color: #374151;">Patterns Checked:</span>
                            <span style="padding: 4px 8px; background: #f3f4f6; color: #374151; border-radius: 4px; font-size: 12px;">
                                ${detectionResults.patternsUsed.length}
                            </span>
                        </div>
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                            <span style="font-weight: 500; color: #374151;">Placeholders Found:</span>
                            <span style="padding: 4px 8px; background: #f3f4f6; color: #374151; border-radius: 4px; font-size: 12px;">
                                ${uniquePlaceholders.length}
                            </span>
                        </div>
                        <div style="display: flex; justify-content: space-between; align-items: center;">
                            <span style="font-weight: 500; color: #374151;">Matched Columns:</span>
                            <span style="padding: 4px 8px; background: #dcfce7; color: #166534; border-radius: 4px; font-size: 12px;">
                                ${mappingAnalysis.matched.length}
                            </span>
                        </div>
                    </div>
                    
                    <div id="placeholderList" style="max-height: 300px; overflow-y: auto;">
                        ${uniquePlaceholders.map((placeholder, index) => {
                            const isMatched = mappingAnalysis.matched.includes(placeholder.name);
                            const suggestions = mappingAnalysis.suggestions[placeholder.name] || [];
                            const mappedColumn = mappingAnalysis.columnMappings && mappingAnalysis.columnMappings[placeholder.name];
                            const confidence = placeholder.confidence || 0.5;
                            const detectedBy = placeholder.detectedBy || 'unknown';
                            
                            return `
                                <div class="placeholder-item ${isMatched ? 'matched' : 'unmatched'}" 
                                     data-placeholder="${placeholder.name}" 
                                     data-index="${index}"
                                     draggable="true"
                                     style="margin-bottom: 12px; padding: 12px; background: white; border: 1px solid ${isMatched ? '#dcfce7' : '#fecaca'}; border-radius: 6px; cursor: grab; transition: all 0.2s;">
                                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                                        <div style="display: flex; align-items: center; gap: 8px;">
                                            <span style="font-size: 12px; color: #6b7280;">⋮⋮</span>
                                            <span style="font-family: monospace; font-weight: 600; color: #1e293b;">${placeholder.full}</span>
                                            <span style="padding: 2px 4px; background: ${confidence > 0.8 ? '#dcfce7' : confidence > 0.6 ? '#fef3c7' : '#fecaca'}; color: ${confidence > 0.8 ? '#166534' : confidence > 0.6 ? '#92400e' : '#dc2626'}; border-radius: 2px; font-size: 10px;">
                                                ${Math.round(confidence * 100)}%
                                            </span>
                                        </div>
                                        <div style="display: flex; align-items: center; gap: 8px;">
                                            ${mappedColumn ? `
                                                <span style="padding: 2px 6px; background: #dbeafe; color: #1e40af; border-radius: 3px; font-size: 11px;">
                                                    → ${mappedColumn}
                                                </span>
                                            ` : ''}
                                            <span style="padding: 2px 6px; background: ${isMatched ? '#dcfce7' : '#fecaca'}; color: ${isMatched ? '#166534' : '#dc2626'}; border-radius: 3px; font-size: 11px;">
                                                ${isMatched ? '✓ Matched' : '✗ No Match'}
                                            </span>
                                        </div>
                                    </div>
                                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                                        <div style="font-size: 11px; color: #6b7280;">
                                            Detected by: ${detectedBy.replace('_', ' ')} pattern
                                        </div>
                                        <div style="font-size: 11px; color: #6b7280;">
                                            ${placeholder.pattern ? `${placeholder.pattern.start}${placeholder.pattern.end}` : 'Unknown pattern'}
                                        </div>
                                    </div>
                                    ${!isMatched && suggestions.length > 0 ? `
                                        <div style="margin-top: 8px;">
                                            <div style="font-size: 12px; color: #6b7280; margin-bottom: 4px;">Quick suggestions:</div>
                                            <div style="display: flex; flex-wrap: wrap; gap: 4px;">
                                                ${suggestions.map(suggestion => `
                                                    <button onclick="quickMapPlaceholder('${placeholder.name}', '${suggestion}')" 
                                                            style="padding: 2px 6px; background: #e0e7ff; color: #3730a3; border: 1px solid #c7d2fe; border-radius: 3px; font-size: 11px; cursor: pointer;">
                                                        ${suggestion}
                                                    </button>
                                                `).join('')}
                                            </div>
                                        </div>
                                    ` : ''}
                                </div>
                            `;
                        }).join('')}
                    </div>
                </div>
                
                <!-- Right Panel: CSV Columns -->
                <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px;">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
                        <h3 style="margin: 0; color: #374151; font-size: 18px;">📊 CSV Columns</h3>
                        <div style="display: flex; gap: 8px;">
                            <button id="clearAllMappings" style="padding: 4px 8px; background: #6b7280; color: white; border: none; border-radius: 3px; cursor: pointer; font-size: 11px;">
                                Clear All
                            </button>
                            <button id="autoMapAll" style="padding: 4px 8px; background: #059669; color: white; border: none; border-radius: 3px; cursor: pointer; font-size: 11px;">
                                Auto Map
                            </button>
                        </div>
                    </div>
                    
                    <div style="margin-bottom: 16px;">
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                            <span style="font-weight: 500; color: #374151;">Total Columns:</span>
                            <span style="padding: 4px 8px; background: #f3f4f6; color: #374151; border-radius: 4px; font-size: 12px;">
                                ${csvColumns.length}
                            </span>
                        </div>
                        <div style="display: flex; justify-content: space-between; align-items: center;">
                            <span style="font-weight: 500; color: #374151;">Used Columns:</span>
                            <span style="padding: 4px 8px; background: #dcfce7; color: #166534; border-radius: 4px; font-size: 12px;">
                                ${mappingAnalysis.matched.length}
                            </span>
                        </div>
                    </div>
                    
                    <div id="columnList" style="max-height: 300px; overflow-y: auto;">
                        ${csvColumns.map(column => {
                            const isUsed = mappingAnalysis.matched.includes(column);
                            const isUnused = !isUsed && !mappingAnalysis.unusedPlaceholders.includes(column);
                            const mappedPlaceholders = mappingAnalysis.columnMappings ? 
                                Object.keys(mappingAnalysis.columnMappings).filter(p => mappingAnalysis.columnMappings[p] === column) : [];
                            
                            return `
                                <div class="column-item ${isUsed ? 'used' : isUnused ? 'unused' : 'available'}" 
                                     data-column="${column}" 
                                     style="margin-bottom: 8px; padding: 8px; background: white; border: 1px solid ${isUsed ? '#dcfce7' : isUnused ? '#fef3c7' : '#e5e7eb'}; border-radius: 4px; transition: all 0.2s; cursor: pointer;"
                                     onclick="selectColumnForMapping('${column}')">
                                    <div style="display: flex; justify-content: space-between; align-items: center;">
                                        <div style="display: flex; align-items: center; gap: 8px;">
                                            <span style="font-family: monospace; font-weight: 500; color: #1e293b;">${column}</span>
                                            ${mappedPlaceholders.length > 0 ? `
                                                <span style="padding: 2px 6px; background: #dbeafe; color: #1e40af; border-radius: 3px; font-size: 10px;">
                                                    ← ${mappedPlaceholders.join(', ')}
                                                </span>
                                            ` : ''}
                                        </div>
                                        <span style="padding: 2px 6px; background: ${isUsed ? '#dcfce7' : isUnused ? '#fef3c7' : '#f3f4f6'}; color: ${isUsed ? '#166534' : isUnused ? '#92400e' : '#6b7280'}; border-radius: 3px; font-size: 11px;">
                                            ${isUsed ? '✓ Used' : isUnused ? '⚠ Unused' : '○ Available'}
                                        </span>
                                    </div>
                                </div>
                            `;
                        }).join('')}
                    </div>
                </div>
            </div>
            
            <!-- Smart Suggestions Panel -->
            <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin-bottom: 20px;">
                <h3 style="margin: 0 0 16px 0; color: #374151; font-size: 18px;">🧠 Smart Suggestions</h3>
                
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 16px; margin-bottom: 16px;">
                    <!-- Exact Matches -->
                    ${smartSuggestions.exactMatches.length > 0 ? `
                        <div style="background: white; border: 1px solid #dcfce7; border-radius: 6px; padding: 12px;">
                            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
                                <span style="font-size: 16px;">✅</span>
                                <span style="font-weight: 600; color: #166534;">Exact Matches</span>
                                <span style="padding: 2px 6px; background: #dcfce7; color: #166534; border-radius: 3px; font-size: 11px;">
                                    ${smartSuggestions.exactMatches.length}
                                </span>
                            </div>
                            <div style="font-size: 12px; color: #6b7280;">
                                ${smartSuggestions.exactMatches.map(match => `
                                    <div style="margin-bottom: 4px;">
                                        <span style="font-family: monospace; font-weight: 500;">${match.placeholder}</span> 
                                        → <span style="font-family: monospace; font-weight: 500;">${match.column}</span>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}
                    
                    <!-- Fuzzy Matches -->
                    ${smartSuggestions.fuzzyMatches.length > 0 ? `
                        <div style="background: white; border: 1px solid #fef3c7; border-radius: 6px; padding: 12px;">
                            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
                                <span style="font-size: 16px;">🔍</span>
                                <span style="font-weight: 600; color: #92400e;">Fuzzy Matches</span>
                                <span style="padding: 2px 6px; background: #fef3c7; color: #92400e; border-radius: 3px; font-size: 11px;">
                                    ${smartSuggestions.fuzzyMatches.length}
                                </span>
                            </div>
                            <div style="font-size: 12px; color: #6b7280;">
                                ${smartSuggestions.fuzzyMatches.slice(0, 5).map(match => `
                                    <div style="margin-bottom: 4px; display: flex; justify-content: space-between; align-items: center;">
                                        <span>
                                            <span style="font-family: monospace; font-weight: 500;">${match.placeholder}</span> 
                                            → <span style="font-family: monospace; font-weight: 500;">${match.column}</span>
                                        </span>
                                        <span style="padding: 1px 4px; background: #fef3c7; color: #92400e; border-radius: 2px; font-size: 10px;">
                                            ${Math.round(match.confidence * 100)}%
                                        </span>
                                    </div>
                                `).join('')}
                                ${smartSuggestions.fuzzyMatches.length > 5 ? `
                                    <div style="font-size: 11px; color: #9ca3af; margin-top: 4px;">
                                        +${smartSuggestions.fuzzyMatches.length - 5} more...
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    ` : ''}
                    
                    <!-- Semantic Matches -->
                    ${smartSuggestions.semanticMatches.length > 0 ? `
                        <div style="background: white; border: 1px solid #dbeafe; border-radius: 6px; padding: 12px;">
                            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
                                <span style="font-size: 16px;">🧠</span>
                                <span style="font-weight: 600; color: #1e40af;">Semantic Matches</span>
                                <span style="padding: 2px 6px; background: #dbeafe; color: #1e40af; border-radius: 3px; font-size: 11px;">
                                    ${smartSuggestions.semanticMatches.length}
                                </span>
                            </div>
                            <div style="font-size: 12px; color: #6b7280;">
                                ${smartSuggestions.semanticMatches.slice(0, 5).map(match => `
                                    <div style="margin-bottom: 4px;">
                                        <span style="font-family: monospace; font-weight: 500;">${match.placeholder}</span> 
                                        → <span style="font-family: monospace; font-weight: 500;">${match.column}</span>
                                        <div style="font-size: 10px; color: #9ca3af;">${match.reason}</div>
                                    </div>
                                `).join('')}
                                ${smartSuggestions.semanticMatches.length > 5 ? `
                                    <div style="font-size: 11px; color: #9ca3af; margin-top: 4px;">
                                        +${smartSuggestions.semanticMatches.length - 5} more...
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    ` : ''}
                    
                    <!-- Pattern Matches -->
                    ${smartSuggestions.patternMatches.length > 0 ? `
                        <div style="background: white; border: 1px solid #e0e7ff; border-radius: 6px; padding: 12px;">
                            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
                                <span style="font-size: 16px;">🔧</span>
                                <span style="font-weight: 600; color: #3730a3;">Pattern Matches</span>
                                <span style="padding: 2px 6px; background: #e0e7ff; color: #3730a3; border-radius: 3px; font-size: 11px;">
                                    ${smartSuggestions.patternMatches.length}
                                </span>
                            </div>
                            <div style="font-size: 12px; color: #6b7280;">
                                ${smartSuggestions.patternMatches.slice(0, 5).map(match => `
                                    <div style="margin-bottom: 4px;">
                                        <span style="font-family: monospace; font-weight: 500;">${match.placeholder}</span> 
                                        → <span style="font-family: monospace; font-weight: 500;">${match.column}</span>
                                        <div style="font-size: 10px; color: #9ca3af;">${match.reason}</div>
                                    </div>
                                `).join('')}
                                ${smartSuggestions.patternMatches.length > 5 ? `
                                    <div style="font-size: 11px; color: #9ca3af; margin-top: 4px;">
                                        +${smartSuggestions.patternMatches.length - 5} more...
                                    </div>
                                ` : ''}
                            </div>
                        </div>
                    ` : ''}
                </div>
                
                <!-- Warnings and Tips -->
                ${smartSuggestions.conflictWarnings.length > 0 || smartSuggestions.optimizationTips.length > 0 ? `
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-top: 20px;">
                        ${smartSuggestions.conflictWarnings.length > 0 ? `
                            <div style="background: #fef2f2; border: 1px solid #fecaca; border-radius: 8px; padding: 16px;">
                                <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 12px;">
                                    <span style="font-size: 18px;">⚠️</span>
                                    <span style="font-weight: 600; color: #dc2626; font-size: 16px;">Warnings</span>
                                    <span style="padding: 2px 6px; background: #fecaca; color: #dc2626; border-radius: 3px; font-size: 11px;">
                                        ${smartSuggestions.conflictWarnings.length}
                                    </span>
                                </div>
                                <div style="font-size: 13px; color: #6b7280; max-height: 120px; overflow-y: auto;">
                                    ${smartSuggestions.conflictWarnings.map(warning => `
                                        <div style="margin-bottom: 6px; padding: 6px 8px; background: white; border-radius: 4px; border-left: 3px solid #fecaca;">
                                            ${warning.message}
                                        </div>
                                    `).join('')}
                                </div>
                            </div>
                        ` : ''}
                        
                        ${smartSuggestions.optimizationTips.length > 0 ? `
                            <div style="background: #f0f9ff; border: 1px solid #bae6fd; border-radius: 8px; padding: 16px;">
                                <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 12px;">
                                    <span style="font-size: 18px;">💡</span>
                                    <span style="font-weight: 600; color: #0369a1; font-size: 16px;">Optimization Tips</span>
                                    <span style="padding: 2px 6px; background: #bae6fd; color: #0369a1; border-radius: 3px; font-size: 11px;">
                                        ${smartSuggestions.optimizationTips.length}
                                    </span>
                                </div>
                                <div style="font-size: 13px; color: #6b7280; max-height: 120px; overflow-y: auto;">
                                    ${smartSuggestions.optimizationTips.map(tip => `
                                        <div style="margin-bottom: 6px; padding: 6px 8px; background: white; border-radius: 4px; border-left: 3px solid #bae6fd;">
                                            <div style="font-weight: 500; color: #0369a1;">${tip.message}</div>
                                            <div style="font-size: 12px; color: #9ca3af; margin-top: 2px;">${tip.suggestion}</div>
                                        </div>
                                    `).join('')}
                                </div>
                            </div>
                        ` : ''}
                    </div>
                ` : ''}
            </div>
            
            <!-- Pattern Detection Summary -->
            <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin-bottom: 20px;">
                <h3 style="margin: 0 0 16px 0; color: #374151; font-size: 18px;">🔍 Pattern Detection Results</h3>
                
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 20px;">
                    ${detectionResults.patternsUsed && detectionResults.patternsUsed.length > 0 ? 
                        detectionResults.patternsUsed.map(patternName => {
                            const patternResults = detectionResults.patternResults[patternName] || [];
                            const patternInfo = getAllPlaceholderPatterns(settings).find(p => p.name === patternName);
                            const patternDisplay = patternInfo ? `${patternInfo.start}Field${patternInfo.end}` : patternName;
                            
                            return `
                                <div style="text-align: center; padding: 16px; background: white; border-radius: 8px; border: 1px solid #e5e7eb; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);">
                                    <div style="font-size: 14px; font-weight: 600; color: #1e293b; margin-bottom: 8px;">${patternDisplay}</div>
                                    <div style="font-size: 24px; font-weight: bold; color: #2563eb; margin-bottom: 4px;">${patternResults.length}</div>
                                    <div style="font-size: 12px; color: #6b7280;">placeholders found</div>
                                </div>
                            `;
                        }).join('') :
                        '<div style="text-align: center; color: #6b7280; font-style: italic; padding: 40px; background: white; border-radius: 8px; border: 1px solid #e5e7eb;">No pattern detection results available</div>'
                    }
                </div>
                
                ${detectionResults.suggestions.length > 0 ? `
                    <div style="background: #fef3c7; border: 1px solid #f59e0b; border-radius: 6px; padding: 12px;">
                        <div style="font-weight: 600; color: #92400e; margin-bottom: 8px;">💡 Additional Suggestions</div>
                        <div style="font-size: 12px; color: #92400e;">
                            Found ${detectionResults.suggestions.length} potential placeholders that weren't detected with current patterns:
                            <div style="margin-top: 8px; display: flex; flex-wrap: wrap; gap: 4px;">
                                ${detectionResults.suggestions.map(suggestion => `
                                    <span style="padding: 2px 6px; background: #fbbf24; color: #92400e; border-radius: 3px; font-size: 11px;">
                                        ${suggestion.name}
                                    </span>
                                `).join('')}
                            </div>
                        </div>
                    </div>
                ` : ''}
            </div>
            
            <!-- Bottom Panel: Summary & Actions -->
            <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px;">
                <h3 style="margin: 0 0 16px 0; color: #374151; font-size: 18px;">📈 Validation Summary</h3>
                
                <!-- Tabbed Interface -->
                <div style="margin-bottom: 16px;">
                    <!-- Tab Headers -->
                    <div style="display: flex; border-bottom: 2px solid #e5e7eb; margin-bottom: 16px;">
                        <button class="tab-button active" data-tab="placeholders" onclick="switchTab('placeholders')" style="flex: 1; padding: 12px 16px; background: #f8fafc; border: none; border-bottom: 3px solid #3b82f6; color: #1e40af; font-weight: 600; cursor: pointer; transition: all 0.2s;">
                            📋 Placeholders (${uniquePlaceholders.length})
                        </button>
                        <button class="tab-button" data-tab="mapped" onclick="switchTab('mapped')" style="flex: 1; padding: 12px 16px; background: #f8fafc; border: none; border-bottom: 3px solid transparent; color: #6b7280; font-weight: 600; cursor: pointer; transition: all 0.2s;">
                            ✅ Mapped (${mappingAnalysis.matched.length})
                        </button>
                        <button class="tab-button" data-tab="unmapped" onclick="switchTab('unmapped')" style="flex: 1; padding: 12px 16px; background: #f8fafc; border: none; border-bottom: 3px solid transparent; color: #6b7280; font-weight: 600; cursor: pointer; transition: all 0.2s;">
                            ❌ Unmapped (${mappingAnalysis.unmatched.length})
                        </button>
                        <button class="tab-button" data-tab="columns" onclick="switchTab('columns')" style="flex: 1; padding: 12px 16px; background: #f8fafc; border: none; border-bottom: 3px solid transparent; color: #6b7280; font-weight: 600; cursor: pointer; transition: all 0.2s;">
                            📊 Columns (${csvColumns.length})
                        </button>
                    </div>
                    
                    <!-- Tab Content -->
                    <div id="tabContent">
                        <!-- Placeholders Tab (Active by default) -->
                        <div id="placeholdersDetail" class="tab-content active" style="display: block;">
                        <h4 style="margin: 0 0 12px 0; color: #374151; font-size: 16px;">📋 All Placeholders</h4>
                        <div style="background: white; border: 1px solid #e5e7eb; border-radius: 6px; max-height: 250px; overflow-y: auto;">
                            <table style="width: 100%; border-collapse: collapse; font-size: 12px;">
                                <thead style="background: #f9fafb; position: sticky; top: 0;">
                                    <tr>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Placeholder</th>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Confidence</th>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Mapped To</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${uniquePlaceholders.map((placeholder, index) => {
                                        const mappedColumn = mappingAnalysis.columnMappings && mappingAnalysis.columnMappings[placeholder.name];
                                        return `
                                            <tr style="border-bottom: 1px solid #f3f4f6;">
                                                <td style="padding: 8px; font-family: monospace; font-weight: 500; color: #1e293b;">${placeholder.full}</td>
                                                <td style="padding: 8px;">
                                                    <span style="padding: 2px 4px; background: ${placeholder.confidence > 0.8 ? '#dcfce7' : placeholder.confidence > 0.6 ? '#fef3c7' : '#fecaca'}; color: ${placeholder.confidence > 0.8 ? '#166534' : placeholder.confidence > 0.6 ? '#92400e' : '#dc2626'}; border-radius: 2px; font-size: 10px;">
                                                        ${Math.round((placeholder.confidence || 0.5) * 100)}%
                                                    </span>
                                                </td>
                                                <td style="padding: 8px;">
                                                    ${mappedColumn ? 
                                                        `<span style="padding: 2px 4px; background: #dbeafe; color: #1e40af; border-radius: 2px; font-size: 10px;">${mappedColumn}</span>` :
                                                        '<span style="color: #9ca3af; font-size: 10px;">Not mapped</span>'
                                                    }
                                                </td>
                                            </tr>
                                        `;
                                    }).join('')}
                                </tbody>
                            </table>
                        </div>
                    </div>
                    
                        <!-- Mapped Tab -->
                        <div id="mappedDetail" class="tab-content" style="display: none;">
                        <h4 style="margin: 0 0 12px 0; color: #374151; font-size: 16px;">✅ Mapped Placeholders</h4>
                        <div style="background: white; border: 1px solid #e5e7eb; border-radius: 6px; max-height: 250px; overflow-y: auto;">
                            <table style="width: 100%; border-collapse: collapse; font-size: 12px;">
                                <thead style="background: #f9fafb; position: sticky; top: 0;">
                                    <tr>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Placeholder</th>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Mapped To</th>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Match Type</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${mappingAnalysis.matched.map(placeholderName => {
                                        const placeholder = uniquePlaceholders.find(p => p.name === placeholderName);
                                        const mappedColumn = mappingAnalysis.columnMappings && mappingAnalysis.columnMappings[placeholderName];
                                        const matchType = mappedColumn === placeholderName ? 'Exact' : 'Fuzzy';
                                        return `
                                            <tr style="border-bottom: 1px solid #f3f4f6;">
                                                <td style="padding: 8px; font-family: monospace; font-weight: 500; color: #1e293b;">${placeholder ? placeholder.full : `[${placeholderName}]`}</td>
                                                <td style="padding: 8px; color: #1e40af; font-weight: 500;">${mappedColumn}</td>
                                                <td style="padding: 8px;">
                                                    <span style="padding: 2px 4px; background: ${matchType === 'Exact' ? '#dcfce7' : '#fef3c7'}; color: ${matchType === 'Exact' ? '#166534' : '#92400e'}; border-radius: 2px; font-size: 10px;">
                                                        ${matchType}
                                                    </span>
                                                </td>
                                            </tr>
                                        `;
                                    }).join('')}
                                </tbody>
                            </table>
                        </div>
                    </div>
                    
                        <!-- Unmapped Tab -->
                        <div id="unmappedDetail" class="tab-content" style="display: none;">
                        <h4 style="margin: 0 0 12px 0; color: #374151; font-size: 16px;">❌ Unmapped Placeholders</h4>
                        <div style="background: white; border: 1px solid #e5e7eb; border-radius: 6px; max-height: 250px; overflow-y: auto;">
                            <table style="width: 100%; border-collapse: collapse; font-size: 12px;">
                                <thead style="background: #f9fafb; position: sticky; top: 0;">
                                    <tr>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Placeholder</th>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Suggestions</th>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${mappingAnalysis.unmatched.map(placeholderName => {
                                        const placeholder = uniquePlaceholders.find(p => p.name === placeholderName);
                                        const suggestions = mappingAnalysis.suggestions[placeholderName] || [];
                                        return `
                                            <tr style="border-bottom: 1px solid #f3f4f6;">
                                                <td style="padding: 8px; font-family: monospace; font-weight: 500; color: #1e293b;">${placeholder ? placeholder.full : `[${placeholderName}]`}</td>
                                                <td style="padding: 8px;">
                                                    ${suggestions.length > 0 ? 
                                                        suggestions.slice(0, 2).map(suggestion => 
                                                            `<span style="padding: 2px 4px; background: #f3f4f6; color: #374151; border-radius: 2px; font-size: 10px; margin-right: 4px;">${suggestion}</span>`
                                                        ).join('') :
                                                        '<span style="color: #9ca3af; font-size: 10px;">No suggestions</span>'
                                                    }
                                                </td>
                                                <td style="padding: 8px;">
                                                    <button onclick="mapPlaceholder('${placeholderName}')" style="padding: 2px 6px; background: #3b82f6; color: white; border: none; border-radius: 2px; font-size: 10px; cursor: pointer;">
                                                        Map
                                                    </button>
                                                </td>
                                            </tr>
                                        `;
                                    }).join('')}
                                </tbody>
                            </table>
                        </div>
                    </div>
                    
                        <!-- Columns Tab -->
                        <div id="columnsDetail" class="tab-content" style="display: none;">
                        <h4 style="margin: 0 0 12px 0; color: #374151; font-size: 16px;">📊 CSV Columns</h4>
                        <div style="background: white; border: 1px solid #e5e7eb; border-radius: 6px; max-height: 250px; overflow-y: auto;">
                            <table style="width: 100%; border-collapse: collapse; font-size: 12px;">
                                <thead style="background: #f9fafb; position: sticky; top: 0;">
                                    <tr>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Column Name</th>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Status</th>
                                        <th style="padding: 8px; text-align: left; border-bottom: 1px solid #e5e7eb; font-weight: 600; color: #374151;">Mapped To</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${csvColumns.map(column => {
                                        const mappedPlaceholder = Object.keys(mappingAnalysis.columnMappings || {}).find(key => mappingAnalysis.columnMappings[key] === column);
                                        const isUsed = !!mappedPlaceholder; // A column is used if it has a mapped placeholder
                                        return `
                                            <tr style="border-bottom: 1px solid #f3f4f6;">
                                                <td style="padding: 8px; font-family: monospace; font-weight: 500; color: #1e293b;">${column}</td>
                                                <td style="padding: 8px;">
                                                    <span style="padding: 2px 4px; background: ${isUsed ? '#dcfce7' : '#fef2f2'}; color: ${isUsed ? '#166534' : '#dc2626'}; border-radius: 2px; font-size: 10px;">
                                                        ${isUsed ? 'Used' : 'Unused'}
                                                    </span>
                                                </td>
                                                <td style="padding: 8px;">
                                                    ${mappedPlaceholder ? 
                                                        `<span style="padding: 2px 4px; background: #dbeafe; color: #1e40af; border-radius: 2px; font-size: 10px;">[${mappedPlaceholder}]</span>` :
                                                        '<span style="color: #9ca3af; font-size: 10px;">Not mapped</span>'
                                                    }
                                                </td>
                                            </tr>
                                        `;
                                    }).join('')}
                                </tbody>
                            </table>
                        </div>
                        </div>
                    </div>
                </div>
                
                <div style="display: flex; gap: 12px; justify-content: flex-end;">
                    <button id="refreshValidation" style="padding: 8px 16px; background: #6b7280; color: white; border: none; border-radius: 4px; cursor: pointer;">
                        🔄 Refresh Analysis
                    </button>
                    <button id="processWithValidation" style="padding: 8px 16px; background: #2563eb; color: white; border: none; border-radius: 4px; cursor: pointer;">
                        🚀 Process Files
                    </button>
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Initialize mapping state
    let currentMappings = {};
    let selectedPlaceholder = null;
    let isMappingMode = false;
    
    // Initialize mappings with exact matches
    uniquePlaceholders.forEach(placeholder => {
        const exactMatch = csvColumns.find(col => col.toLowerCase() === placeholder.name.toLowerCase());
        if (exactMatch) {
            currentMappings[placeholder.name] = exactMatch;
        }
    });
    
    // Event handlers
    document.getElementById('closeValidation').onclick = () => {
        modal.remove();
        // Clean up to prevent memory leaks
        modal.onclick = null;
    };
    
    document.getElementById('refreshValidation').onclick = () => {
        modal.remove();
        generatePreview();
    };
    
    document.getElementById('processWithValidation').onclick = () => {
        modal.remove();
        processFiles();
    };
    
    // Mapping mode toggle
    document.getElementById('toggleMappingMode').onclick = () => {
        isMappingMode = !isMappingMode;
        const button = document.getElementById('toggleMappingMode');
        if (isMappingMode) {
            button.textContent = '🎯 Mapping Mode';
            button.style.background = '#059669';
            enableMappingMode();
            updateStatus('Mapping mode enabled. Click placeholders and columns to map them.', 'info');
        } else {
            button.textContent = '📋 View Mode';
            button.style.background = '#6b7280';
            disableMappingMode();
            updateStatus('Mapping mode disabled.', 'info');
        }
    };
    
    // Clear all mappings
    document.getElementById('clearAllMappings').onclick = () => {
        currentMappings = {};
        updateMappingDisplay();
        updateStatus('All mappings cleared', 'info');
    };
    
    // Auto map all
    document.getElementById('autoMapAll').onclick = () => {
        currentMappings = {};
        let mappedCount = 0;
        
        uniquePlaceholders.forEach(placeholder => {
            // Try exact match first
            let match = csvColumns.find(col => col.toLowerCase() === placeholder.name.toLowerCase());
            
            // If no exact match, try fuzzy matching
            if (!match) {
                const fuzzyMatches = smartSuggestions.fuzzyMatches.filter(f => f.placeholder === placeholder.name);
                if (fuzzyMatches.length > 0 && fuzzyMatches[0].confidence > 0.7) {
                    match = fuzzyMatches[0].column;
                }
            }
            
            if (match) {
                currentMappings[placeholder.name] = match;
                mappedCount++;
            }
        });
        
        updateMappingDisplay();
        updateMappingStatistics();
        updateStatus(`Auto-mapped ${mappedCount} placeholders`, 'success');
    };
    
    // Drag and drop functionality
    function enableMappingMode() {
        // Add drag event listeners to placeholders
        document.querySelectorAll('.placeholder-item').forEach(item => {
            item.addEventListener('dragstart', handleDragStart);
            item.addEventListener('dragend', handleDragEnd);
        });
        
        // Add drop event listeners to columns
        document.querySelectorAll('.column-item').forEach(item => {
            item.addEventListener('dragover', handleDragOver);
            item.addEventListener('drop', handleDrop);
            item.addEventListener('dragenter', handleDragEnter);
            item.addEventListener('dragleave', handleDragLeave);
        });
        
        // Add click handlers for column selection
        document.querySelectorAll('.column-item').forEach(item => {
            item.addEventListener('click', handleColumnClick);
        });
    }
    
    function disableMappingMode() {
        // Remove drag event listeners
        document.querySelectorAll('.placeholder-item').forEach(item => {
            item.removeEventListener('dragstart', handleDragStart);
            item.removeEventListener('dragend', handleDragEnd);
        });
        
        document.querySelectorAll('.column-item').forEach(item => {
            item.removeEventListener('dragover', handleDragOver);
            item.removeEventListener('drop', handleDrop);
            item.removeEventListener('dragenter', handleDragEnter);
            item.removeEventListener('dragleave', handleDragLeave);
            item.removeEventListener('click', handleColumnClick);
        });
        
        // Clear selection
        selectedPlaceholder = null;
        document.querySelectorAll('.placeholder-item, .column-item').forEach(item => {
            item.classList.remove('selected');
        });
    }
    
    function handleDragStart(e) {
        if (!isMappingMode) return;
        selectedPlaceholder = e.target.dataset.placeholder;
        e.target.style.opacity = '0.5';
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', selectedPlaceholder);
    }
    
    function handleDragEnd(e) {
        e.target.style.opacity = '1';
    }
    
    function handleDragOver(e) {
        if (!isMappingMode) return;
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
    }
    
    function handleDragEnter(e) {
        if (!isMappingMode) return;
        e.preventDefault();
        e.target.classList.add('drag-over');
    }
    
    function handleDragLeave(e) {
        if (!isMappingMode) return;
        e.target.classList.remove('drag-over');
    }
    
    function handleDrop(e) {
        if (!isMappingMode) return;
        e.preventDefault();
        e.target.classList.remove('drag-over');
        
        const placeholder = e.dataTransfer.getData('text/plain');
        const column = e.target.dataset.column;
        
        if (placeholder && column) {
            currentMappings[placeholder] = column;
            updateMappingDisplay();
            updateStatus(`Mapped ${placeholder} → ${column}`, 'success');
        }
    }
    
    function handleColumnClick(e) {
        if (!isMappingMode) return;
        e.stopPropagation();
        
        const column = e.target.closest('.column-item').dataset.column;
        
        if (selectedPlaceholder) {
            // Map selected placeholder to clicked column
            currentMappings[selectedPlaceholder] = column;
            updateMappingDisplay();
            updateStatus(`Mapped ${selectedPlaceholder} → ${column}`, 'success');
            
            // Clear selection
            selectedPlaceholder = null;
            document.querySelectorAll('.placeholder-item, .column-item').forEach(item => {
                item.classList.remove('selected');
            });
        } else {
            // Select this column for mapping
            document.querySelectorAll('.column-item').forEach(item => {
                item.classList.remove('selected');
            });
            e.target.closest('.column-item').classList.add('selected');
            selectedPlaceholder = column;
            updateStatus(`Selected column: ${column}. Now click a placeholder to map it.`, 'info');
        }
    }
    
    function updateMappingDisplay() {
        // Update placeholder items
        document.querySelectorAll('.placeholder-item').forEach(item => {
            const placeholder = item.dataset.placeholder;
            const mappedColumn = currentMappings[placeholder];
            
            if (mappedColumn) {
                item.classList.add('matched');
                item.classList.remove('unmatched');
                
                // Update the mapping display
                let mappingSpan = item.querySelector('.mapping-display');
                if (!mappingSpan) {
                    mappingSpan = document.createElement('span');
                    mappingSpan.className = 'mapping-display';
                    mappingSpan.style.cssText = 'padding: 2px 6px; background: #dbeafe; color: #1e40af; border-radius: 3px; font-size: 11px; margin-left: 8px;';
                    item.querySelector('div > div:first-child').appendChild(mappingSpan);
                }
                mappingSpan.textContent = `→ ${mappedColumn}`;
            } else {
                item.classList.remove('matched');
                item.classList.add('unmatched');
                
                // Remove mapping display
                const mappingSpan = item.querySelector('.mapping-display');
                if (mappingSpan) {
                    mappingSpan.remove();
                }
            }
        });
        
        // Update column items
        document.querySelectorAll('.column-item').forEach(item => {
            const column = item.dataset.column;
            const mappedPlaceholders = Object.keys(currentMappings).filter(p => currentMappings[p] === column);
            
            if (mappedPlaceholders.length > 0) {
                item.classList.add('used');
                item.classList.remove('unused', 'available');
                
                // Update the mapping display
                let mappingSpan = item.querySelector('.mapping-display');
                if (!mappingSpan) {
                    mappingSpan = document.createElement('span');
                    mappingSpan.className = 'mapping-display';
                    mappingSpan.style.cssText = 'padding: 2px 6px; background: #dbeafe; color: #1e40af; border-radius: 3px; font-size: 10px; margin-left: 8px;';
                    item.querySelector('div > div:first-child').appendChild(mappingSpan);
                }
                mappingSpan.textContent = `← ${mappedPlaceholders.join(', ')}`;
            } else {
                item.classList.remove('used');
                item.classList.add('available');
                
                // Remove mapping display
                const mappingSpan = item.querySelector('.mapping-display');
                if (mappingSpan) {
                    mappingSpan.remove();
                }
            }
        });
        
        // Update statistics
        updateMappingStatistics();
    }
    
    function updateMappingStatistics() {
        const matchedCount = Object.keys(currentMappings).length;
        const unmatchedCount = uniquePlaceholders.length - matchedCount;
        const usedColumns = new Set(Object.values(currentMappings)).size;
        const unusedColumns = csvColumns.length - usedColumns;
        
        // Update the summary cards
        const summaryCards = document.querySelectorAll('.stat-card');
        if (summaryCards.length >= 4) {
            summaryCards[1].querySelector('.stat-number').textContent = matchedCount;
            summaryCards[2].querySelector('.stat-number').textContent = unmatchedCount;
            summaryCards[3].querySelector('.stat-number').textContent = unusedColumns;
        }
    }
    
    // Close on background click
    modal.onclick = function(e) {
        if (e.target === modal) {
            modal.remove();
        }
    };
}

// Helper functions for template validation
function extractPlaceholdersFromTemplate(previewData) {
    // This would ideally extract placeholders from the actual template content
    // For now, we'll simulate based on the preview data
    const settings = loadSettings();
    const pattern = getPlaceholderPattern(settings);
    
    // Extract from filename pattern
    const filenamePlaceholders = extractPlaceholders(previewData.previewName || '', pattern);
    
    // Extract from fields (simulated template content)
    const fieldPlaceholders = Object.keys(previewData.fields || {}).map(field => ({
        full: `${pattern.start}${field}${pattern.end}`,
        name: field,
        start: 0,
        end: 0
    }));
    
    return [...filenamePlaceholders, ...fieldPlaceholders];
}

function extractPlaceholdersFromTemplateContent(previewData, settings) {
    const placeholders = [];
    const pattern = getPlaceholderPattern(settings);
    
    // Use the template placeholders extracted by the backend
    if (previewData.templatePlaceholders && previewData.templatePlaceholders.length > 0) {
        previewData.templatePlaceholders.forEach(placeholderName => {
            placeholders.push({
                full: `${pattern.start}${placeholderName}${pattern.end}`,
                name: placeholderName,
                start: 0,
                end: 0,
                pattern: { start: pattern.start, end: pattern.end },
                detectedBy: 'template_content',
                confidence: 0.95, // Very high confidence for placeholders found in actual template
                source: 'template'
            });
        });
    }
    
    // Fallback to fields data if template placeholders not available
    else if (previewData.fields) {
        Object.keys(previewData.fields).forEach(field => {
            placeholders.push({
                full: `${pattern.start}${field}${pattern.end}`,
                name: field,
                start: 0,
                end: 0,
                pattern: { start: pattern.start, end: pattern.end },
                detectedBy: 'template_content',
                confidence: 0.8, // Lower confidence for fallback
                source: 'template'
            });
        });
    }
    
    return placeholders;
}

// Helper function to create simulated template content for pattern detection
function createSimulatedTemplateContent(templatePlaceholders) {
    if (!templatePlaceholders || templatePlaceholders.length === 0) return '';
    
    // Create a simulated template content with placeholders in different patterns
    const patterns = [
        { start: '[', end: ']' },
        { start: '[[', end: ']]' },
        { start: '{', end: '}' },
        { start: '%', end: '%' },
        { start: '(', end: ')' },
        { start: '<', end: '>' }
    ];
    
    let content = '';
    templatePlaceholders.forEach((placeholder, index) => {
        const pattern = patterns[index % patterns.length];
        content += `This is a document with ${pattern.start}${placeholder}${pattern.end} placeholder. `;
    });
    
    return content;
}

// Global functions for tabbed interface
window.switchTab = function(tabName) {
    // Hide all tab content
    const allTabContent = document.querySelectorAll('.tab-content');
    allTabContent.forEach(content => content.style.display = 'none');
    
    // Remove active class from all tab buttons
    const allTabButtons = document.querySelectorAll('.tab-button');
    allTabButtons.forEach(button => {
        button.classList.remove('active');
        button.style.borderBottom = '3px solid transparent';
        button.style.color = '#6b7280';
    });
    
    // Show selected tab content
    const targetContent = document.getElementById(tabName + 'Detail');
    if (targetContent) {
        targetContent.style.display = 'block';
    }
    
    // Activate selected tab button
    const targetButton = document.querySelector(`[data-tab="${tabName}"]`);
    if (targetButton) {
        targetButton.classList.add('active');
        targetButton.style.borderBottom = '3px solid #3b82f6';
        targetButton.style.color = '#1e40af';
    }
};

window.mapPlaceholder = function(placeholderName) {
    updateStatus(`Mapping placeholder: ${placeholderName}`, 'info');
    // This would trigger the mapping mode for this specific placeholder
    // For now, just show a message
    alert(`To map "${placeholderName}", enable mapping mode and click on a CSV column.`);
};

window.suggestMapping = function(columnName) {
    updateStatus(`Suggesting mappings for column: ${columnName}`, 'info');
    // This would show suggestions for this column
    alert(`Suggestions for "${columnName}" would be shown here.`);
};

function analyzePlaceholderMapping(placeholders, csvColumns) {
    const matched = [];
    const unmatched = [];
    const suggestions = {};
    const unusedColumns = [...csvColumns];
    const columnMappings = {};
    
    placeholders.forEach(placeholder => {
        const exactMatch = csvColumns.find(col => col.toLowerCase() === placeholder.name.toLowerCase());
        
        if (exactMatch) {
            matched.push(placeholder.name);
            columnMappings[placeholder.name] = exactMatch;
            const index = unusedColumns.indexOf(exactMatch);
            if (index > -1) unusedColumns.splice(index, 1);
        } else {
            unmatched.push(placeholder.name);
            
            // Generate suggestions
            const placeholderLower = placeholder.name.toLowerCase();
            const suggestionsList = csvColumns.filter(col => 
                col.toLowerCase().includes(placeholderLower) || 
                placeholderLower.includes(col.toLowerCase()) ||
                calculateSimilarity(col.toLowerCase(), placeholderLower) > 0.6
            ).slice(0, 3);
            
            suggestions[placeholder.name] = suggestionsList;
        }
    });
    
    return {
        matched,
        unmatched,
        suggestions,
        unusedColumns,
        unusedPlaceholders: unmatched,
        columnMappings
    };
}

function calculateSimilarity(str1, str2) {
    const longer = str1.length > str2.length ? str1 : str2;
    const shorter = str1.length > str2.length ? str2 : str1;
    
    if (longer.length === 0) return 1.0;
    
    const editDistance = levenshteinDistance(longer, shorter);
    return (longer.length - editDistance) / longer.length;
}

function levenshteinDistance(str1, str2) {
    const matrix = [];
    
    for (let i = 0; i <= str2.length; i++) {
        matrix[i] = [i];
    }
    
    for (let j = 0; j <= str1.length; j++) {
        matrix[0][j] = j;
    }
    
    for (let i = 1; i <= str2.length; i++) {
        for (let j = 1; j <= str1.length; j++) {
            if (str2.charAt(i - 1) === str1.charAt(j - 1)) {
                matrix[i][j] = matrix[i - 1][j - 1];
            } else {
                matrix[i][j] = Math.min(
                    matrix[i - 1][j - 1] + 1,
                    matrix[i][j - 1] + 1,
                    matrix[i - 1][j] + 1
                );
            }
        }
    }
    
    return matrix[str2.length][str1.length];
}

function mapPlaceholder(placeholderName, columnName) {
    // This would implement the mapping functionality
    console.log(`Mapping placeholder "${placeholderName}" to column "${columnName}"`);
    updateStatus(`Mapped ${placeholderName} → ${columnName}`, 'success');
}

// Quick mapping function for suggestion buttons
function quickMapPlaceholder(placeholderName, columnName) {
    // Find the validation modal and update mappings
    const modal = document.querySelector('.modal');
    if (modal) {
        // This would be called from within the validation UI
        console.log(`Quick mapping: ${placeholderName} → ${columnName}`);
    }
}

// Column selection for mapping
function selectColumnForMapping(columnName) {
    // This would be called when clicking on a column in mapping mode
    console.log(`Selected column for mapping: ${columnName}`);
}
