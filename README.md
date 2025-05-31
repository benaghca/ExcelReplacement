# Document Template Processor

A Windows application that processes Excel and Word templates by replacing placeholders with data from a CSV file.

## What it does

This tool allows you to:
- Select a CSV file containing your data
- Select an Excel or Word template containing placeholders
- Preview and validate your template before processing
- Choose an output directory
- Generate individual files for each row in your CSV

## How to use

1. **Build the application**:
   ```
   dotnet build
   ```

2. **Run the application**:
   ```
   dotnet run
   ```

3. **Using the application**:
   - Select the template type (Excel or Word)
   - Select your CSV file with replacement data
   - Select your template file with placeholders
   - Click "Preview Template" to validate your setup
   - Choose an output directory for the generated files
   - Click "Process Files" to start
   - The application will process the files and display progress
   - When complete, you'll see a summary of the processed records

## Template Preview and Validation

The preview feature helps you catch issues before processing large batches:

1. **Validation Tab**:
   - Shows all placeholders found in your template
   - Indicates if each placeholder has a matching CSV column
   - Displays sample values from your CSV
   - Shows the location of each placeholder in your document

2. **Preview Tab**:
   - Shows how your template will look with the first row of data
   - Displays content from all sheets (Excel) or sections (Word)
   - Helps verify formatting and layout

3. **Status Information**:
   - Shows the number of errors and warnings
   - Highlights unused CSV columns
   - Indicates missing placeholders

## Distribution for non-developers

You can distribute the application as a standalone executable that doesn't require .NET SDK installation:

1. **Publish as a self-contained, single-file application**:
   ```
   dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
   ```

2. **Locate the executable**:
   - The executable will be in the `bin\Release\net6.0-windows\win-x64\publish` directory
   - The file will be named `ExcelReplacement.exe`

3. **Distribute to users**:
   - Users can run the application by double-clicking the .exe file
   - No installation or administrative privileges required
   - The application can be run from any location, including USB drives

## File requirements

### CSV File
- Must be properly formatted with headers in the first row
- Must contain the following required fields:
  - Location
  - Facility
  - Equipment
  - Procedure
  - Lineup
- Can contain additional fields as needed

Example CSV format:
```
Location,Facility,Equipment,Procedure,Lineup,Panel-1,Breaker-1,Breaker-2
Location1,FacilityA,Equipment-01,Maintenance,Line1,MP-11,CKT-34/36/38,CKT-28/30/32
```

### Template Files
- Must be a valid .xlsx (Excel) or .docx (Word) file
- Use placeholders in the format `[FieldName]` where FieldName matches a CSV header (case-sensitive, no extra spaces)
- Example placeholders: `[Location]`, `[Facility]`, `[Equipment]`, etc.
- You can combine placeholders: `[Location] [Facility]`
- Placeholders can be used anywhere in your document, including inside tables, headers, and footers
- **Formatting is preserved:** You can use bold, italics, and other formatting in your templates. Only the replaced text will change; the rest of your formatting will remain intact.

## How it works

1. The application reads the CSV file and extracts data for each row
2. For each row, it:
   - Creates a copy of the template
   - Finds text containing placeholders
   - Replaces placeholders with the corresponding data from the CSV
   - Saves the file with a name generated from the CSV data
   - Moves to the next row

## Troubleshooting

### Common issues

1. **File not found errors**:
   - Ensure your CSV and template files exist and are accessible

2. **Format errors**:
   - Check that your CSV is properly formatted with headers
   - Ensure your template is a valid .xlsx or .docx file

3. **Missing fields**:
   - Make sure your CSV contains all required fields
   - Check for spelling mismatches between CSV headers and placeholders

4. **Access denied errors**:
   - Ensure you have write permissions for the output directory

5. **Placeholders not being replaced or formatting issues:**
   - Make sure placeholders use the format `[FieldName]` with single square brackets
   - Placeholders must match CSV headers exactly (case-sensitive, no extra spaces)
   - Placeholders can be inside tables, headers, footers, or split across formatting (bold, italics, etc.)
   - If you see formatting issues, ensure your template is using standard Word formatting and not special objects

## Sample files

The repository includes:
- `test_data.csv`: Sample CSV file with test data
- `template.xlsx`: Sample Excel template with placeholders

You can use these to test the application.

## New in this release
- Template Preview and Validation feature
  - Preview how your template will look with actual data
  - Validate placeholders against CSV columns
  - Catch errors before processing large batches
- Placeholders are now replaced everywhere in your document, including tables, headers, and footers
- Mixed formatting (bold, italics, etc.) is preserved when replacing placeholders
- Output file type matches the template type (Excel or Word)