# Excel Replacement Tool

A Windows application that processes Excel templates by replacing placeholders with data from a CSV file.

## What it does

This tool allows you to:
- Select a CSV file containing your data
- Select an Excel template containing placeholders
- Choose an output directory
- Generate individual Excel files for each row in your CSV

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
   - When prompted, select your CSV file with replacement data
   - Select your Excel template file with placeholders
   - Choose an output directory for the generated files
   - The application will process the files and display progress
   - When complete, you'll see a summary of the processed records

## File requirements

### CSV File
- Must be properly formatted with headers in the first row
- Must contain the following required fields:
  - Site
  - Building
  - Equipment
  - Procedure
  - Lineup
- Can contain additional fields as needed

Example CSV format:
```
Site,Building,Equipment,Procedure,Lineup,Panel-1,Breaker-1,Breaker-2
Site1,BuildingA,Pump-01,Maintenance,Line1,MP-11,CKT-34/36/38,CKT-28/30/32
```

### Excel Template
- Must be a valid .xlsx file
- Use placeholders in the format `[[FieldName]]` where FieldName matches a CSV header
- Example placeholders: `[[FirstName]]`, `[[LastName]]`, `[[Email]]`, etc.
- You can combine placeholders: `[[FirstName]] [[LastName]]`

## How it works

1. The application reads the CSV file and extracts data for each row
2. For each row, it:
   - Creates a copy of the Excel template
   - Finds cells containing placeholders
   - Replaces placeholders with the corresponding data from the CSV
   - Saves the file with a name generated from the CSV data
   - Moves to the next row

## Troubleshooting

### Common issues

1. **File not found errors**:
   - Ensure your CSV and Excel files exist and are accessible

2. **Format errors**:
   - Check that your CSV is properly formatted with headers
   - Ensure your Excel template is a valid .xlsx file

3. **Missing fields**:
   - Make sure your CSV contains all required fields
   - Check for spelling mismatches between CSV headers and placeholders

4. **Access denied errors**:
   - Ensure you have write permissions for the output directory

5. **Placeholders not being replaced**:
   - Make sure placeholders use the format `[[FieldName]]` with double square brackets
   - Verify that the placeholder names exactly match the CSV header names

## Sample files

The repository includes:
- `test_data.csv`: Sample CSV file with test data
- `template.xlsx`: Sample Excel template with placeholders

You can use these to test the application.