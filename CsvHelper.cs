using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;

namespace InnerUtils
{
    [Serializable]
    sealed class CsvFile
    {
        public readonly List<string> Headers = new List<string>();

        public readonly CsvRecords Records = new CsvRecords();

        public int HeaderCount
        {
            get
            {
                return Headers.Count;
            }
        }

        public int RecordCount
        {
            get
            {
                return Records.Count;
            }
        }
        public CsvRecord this[int recordIndex]
        {
            get
            {
                if (recordIndex > (Records.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no record at index {0}.", recordIndex));

                return Records[recordIndex];
            }
        }

        public string this[int recordIndex, int fieldIndex]
        {
            get
            {
                if (recordIndex > (Records.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no record at index {0}.", recordIndex));

                CsvRecord record = Records[recordIndex];
                if (fieldIndex > (record.Fields.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no field at index {0} in record {1}.", fieldIndex, recordIndex));

                return record.Fields[fieldIndex];
            }
            set
            {
                if (recordIndex > (Records.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no record at index {0}.", recordIndex));

                CsvRecord record = Records[recordIndex];

                if (fieldIndex > (record.Fields.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no field at index {0}.", fieldIndex));

                record.Fields[fieldIndex] = value;
            }
        }

        public string this[int recordIndex, string fieldName]
        {
            get
            {
                if (recordIndex > (Records.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no record at index {0}.", recordIndex));

                CsvRecord record = Records[recordIndex];

                int fieldIndex = -1;

                for (int i = 0; i < Headers.Count; i++)
                {
                    if (string.Compare(Headers[i], fieldName) != 0)
                        continue;

                    fieldIndex = i;
                    break;
                }

                if (fieldIndex == -1)
                    throw new ArgumentException(string.Format("There is no field header with the name '{0}'", fieldName));

                if (fieldIndex > (record.Fields.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no field at index {0} in record {1}.", fieldIndex, recordIndex));

                return record.Fields[fieldIndex];
            }
            set
            {
                if (recordIndex > (Records.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no record at index {0}.", recordIndex));

                CsvRecord record = Records[recordIndex];

                int fieldIndex = -1;

                for (int i = 0; i < Headers.Count; i++)
                {
                    if (string.Compare(Headers[i], fieldName) != 0)
                        continue;

                    fieldIndex = i;
                    break;
                }

                if (fieldIndex == -1)
                    throw new ArgumentException(string.Format("There is no field header with the name '{0}'", fieldName));

                if (fieldIndex > (record.Fields.Count - 1))
                    throw new IndexOutOfRangeException(string.Format("There is no field at index {0} in record {1}.", fieldIndex, recordIndex));

                record.Fields[fieldIndex] = value;
            }
        }



        public void Populate(string filePath, bool hasHeaderRow)
        {
            Populate(filePath, null, hasHeaderRow, false);
        }

        public void Populate(string filePath, bool hasHeaderRow, bool trimColumns)
        {
            Populate(filePath, null, hasHeaderRow, trimColumns);
        }

        public void Populate(string filePath, Encoding encoding, bool hasHeaderRow, bool trimColumns)
        {
            using (CsvReader reader = new CsvReader(filePath, encoding) { HasHeaderRow = hasHeaderRow, TrimColumns = trimColumns })
            {
                PopulateCsvFile(reader);
            }
        }

        public void Populate(Stream stream, bool hasHeaderRow)
        {
            Populate(stream, null, hasHeaderRow, false);
        }

        public void Populate(Stream stream, bool hasHeaderRow, bool trimColumns)
        {
            Populate(stream, null, hasHeaderRow, trimColumns);
        }

        public void Populate(Stream stream, Encoding encoding, bool hasHeaderRow, bool trimColumns)
        {
            using (CsvReader reader = new CsvReader(stream, encoding) { HasHeaderRow = hasHeaderRow, TrimColumns = trimColumns })
            {
                PopulateCsvFile(reader);
            }
        }

        public void Populate(bool hasHeaderRow, string csvContent)
        {
            Populate(hasHeaderRow, csvContent, null, false);
        }
        public void Populate(bool hasHeaderRow, string csvContent, bool trimColumns)
        {
            Populate(hasHeaderRow, csvContent, null, trimColumns);
        }

        public void Populate(bool hasHeaderRow, string csvContent, Encoding encoding, bool trimColumns)
        {
            using (CsvReader reader = new CsvReader(encoding, csvContent) { HasHeaderRow = hasHeaderRow, TrimColumns = trimColumns })
            {
                PopulateCsvFile(reader);
            }
        }

        private void PopulateCsvFile(CsvReader reader)
        {
            Headers.Clear();
            Records.Clear();

            bool addedHeader = false;

            while (reader.ReadNextRecord())
            {
                if (reader.HasHeaderRow && !addedHeader)
                {
                    reader.Fields.ForEach(field => Headers.Add(field));
                    addedHeader = true;
                    continue;
                }

                CsvRecord record = new CsvRecord();
                reader.Fields.ForEach(field => record.Fields.Add(field));
                Records.Add(record);
            }
        }


    }

    [Serializable]
    sealed class CsvRecords : List<CsvRecord>
    {
    }

    [Serializable]
    sealed class CsvRecord
    {

        public readonly List<string> Fields = new List<string>();

        public int FieldCount
        {
            get
            {
                return Fields.Count;
            }
        }

    }

    sealed class CsvReader : IDisposable
    {


        private FileStream _fileStream;
        private Stream _stream;
        private StreamReader _streamReader;
        private StreamWriter _streamWriter;
        private Stream _memoryStream;
        private Encoding _encoding;
        private readonly StringBuilder _columnBuilder = new StringBuilder(100);
        private readonly Type _type = Type.File;



        public bool TrimColumns { get; set; }

        public bool HasHeaderRow { get; set; }

        public List<string> Fields { get; private set; }

        public int? FieldCount
        {
            get
            {
                return (Fields != null ? Fields.Count : (int?)null);
            }
        }



        private enum Type
        {
            File,
            Stream,
            String
        }



        public CsvReader(string filePath)
        {
            _type = Type.File;
            Initialise(filePath, Encoding.Default);
        }

        public CsvReader(string filePath, Encoding encoding)
        {
            _type = Type.File;
            Initialise(filePath, encoding);
        }

        public CsvReader(Stream stream)
        {
            _type = Type.Stream;
            Initialise(stream, Encoding.Default);
        }

        public CsvReader(Stream stream, Encoding encoding)
        {
            _type = Type.Stream;
            Initialise(stream, encoding);
        }

        public CsvReader(Encoding encoding, string csvContent)
        {
            _type = Type.String;
            Initialise(encoding, csvContent);
        }



        private void Initialise(string filePath, Encoding encoding)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(string.Format("The file '{0}' does not exist.", filePath));
            _fileStream = File.OpenRead(filePath);
            Initialise(_fileStream, encoding);
        }

        private void Initialise(Stream stream, Encoding encoding)
        {
            if (stream == null)
                throw new ArgumentNullException("The supplied stream is null.");

            _stream = stream;
            _stream.Position = 0;
            _encoding = (encoding ?? Encoding.Default);
            _streamReader = new StreamReader(_stream, _encoding);
        }

        private void Initialise(Encoding encoding, string csvContent)
        {
            if (csvContent == null)
                throw new ArgumentNullException("The supplied csvContent is null.");

            _encoding = (encoding ?? Encoding.Default);

            _memoryStream = new MemoryStream(csvContent.Length);
            _streamWriter = new StreamWriter(_memoryStream);
            _streamWriter.Write(csvContent);
            _streamWriter.Flush();
            Initialise(_memoryStream, encoding);
        }

        public bool ReadNextRecord()
        {
            Fields = null;
            var sb = new StringBuilder();
            while (_streamReader.Peek() >= 0)
            {
                var c = (char)_streamReader.Read();
                if (c == 0x0d) // CR
                {
                    c = (char)_streamReader.Read(); // LF
                    if (c != 0x0a)
                    {
                        throw new Exception("Invalid csv file.");
                    }
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            var line = sb.ToString();

            if (line.Length == 0)
                return false;

            ParseLine(line);
            return true;
        }

        public DataTable ReadIntoDataTable()
        {
            return ReadIntoDataTable(new System.Type[] { });
        }

        public DataTable ReadIntoDataTable(System.Type[] columnTypes)
        {
            var dataTable = new DataTable();
            bool addedHeader = false;
            _stream.Position = 0;

            while (ReadNextRecord())
            {
                if (!addedHeader)
                {
                    for (var i = 0; i < Fields.Count; i++)
                        dataTable.Columns.Add(Fields[i], (columnTypes.Length > 0 ? columnTypes[i] : typeof(string)));

                    addedHeader = true;
                    continue;
                }

                var row = dataTable.NewRow();

                for (var i = 0; i < Fields.Count; i++)
                    row[i] = Fields[i];

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private void ParseLine(string line)
        {
            Fields = new List<string>();
            var inColumn = false;
            var inQuotes = false;
            _columnBuilder.Remove(0, _columnBuilder.Length);

            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];

                if (!inColumn)
                {
                    if (character == '"')
                        inQuotes = true;
                    else if (_columnBuilder.Length == 0 && character == ',')
                    {
                        Fields.Add(string.Empty); // Add empty item.
                        continue;
                    }
                    else
                        _columnBuilder.Append(character);

                    inColumn = true;
                    continue;
                }

                if (inQuotes)
                {
                    if (character == '"' && ((line.Length > (i + 1) && line[i + 1] == ',') || ((i + 1) == line.Length)))
                    {
                        inQuotes = false;
                        inColumn = false;
                        i++;
                    }
                    else if (character == '"' && line.Length > (i + 1) && line[i + 1] == '"')
                        i++;
                }
                else if (character == ',')
                    inColumn = false;

                if (!inColumn)
                {
                    Fields.Add(TrimColumns ? _columnBuilder.ToString().Trim() : _columnBuilder.ToString());
                    _columnBuilder.Remove(0, _columnBuilder.Length);
                }
                else // append the current column
                    _columnBuilder.Append(character);
            }

            if (inColumn)
                Fields.Add(TrimColumns ? _columnBuilder.ToString().Trim() : _columnBuilder.ToString());
        }

        public void Dispose()
        {
            if (_streamReader != null)
            {
                _streamReader.Close();
                _streamReader.Dispose();
            }

            if (_streamWriter != null)
            {
                _streamWriter.Close();
                _streamWriter.Dispose();
            }

            if (_memoryStream != null)
            {
                _memoryStream.Close();
                _memoryStream.Dispose();
            }

            if (_fileStream != null)
            {
                _fileStream.Close();
                _fileStream.Dispose();
            }

            if ((_type == Type.String || _type == Type.File) && _stream != null)
            {
                _stream.Close();
                _stream.Dispose();
            }
        }


    }

    sealed class CsvWriter : IDisposable
    {


        private StreamWriter _streamWriter;
        private bool _replaceCarriageReturnsAndLineFeedsFromFieldValues = true;
        private string _carriageReturnAndLineFeedReplacement = ",";



        public bool ReplaceCarriageReturnsAndLineFeedsFromFieldValues
        {
            get
            {
                return _replaceCarriageReturnsAndLineFeedsFromFieldValues;
            }
            set
            {
                _replaceCarriageReturnsAndLineFeedsFromFieldValues = value;
            }
        }

        public string CarriageReturnAndLineFeedReplacement
        {
            get
            {
                return _carriageReturnAndLineFeedReplacement;
            }
            set
            {
                _carriageReturnAndLineFeedReplacement = value;
            }
        }




        public void WriteCsv(CsvFile csvFile, string filePath)
        {
            WriteCsv(csvFile, filePath, null);
        }

        public void WriteCsv(CsvFile csvFile, string filePath, Encoding encoding)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, false, encoding ?? Encoding.Default))
            {
                WriteToStream(csvFile, writer);
                writer.Flush();
                writer.Close();
            }
        }

        public void WriteCsv(CsvFile csvFile, Stream stream)
        {
            WriteCsv(csvFile, stream, null);
        }

        public void WriteCsv(CsvFile csvFile, Stream stream, Encoding encoding)
        {
            stream.Position = 0;
            _streamWriter = new StreamWriter(stream, encoding ?? Encoding.Default);
            WriteToStream(csvFile, _streamWriter);
            _streamWriter.Flush();
            stream.Position = 0;
        }

        public string WriteCsv(CsvFile csvFile, Encoding encoding)
        {
            string content = string.Empty;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(memoryStream, encoding ?? Encoding.Default))
                {
                    WriteToStream(csvFile, writer);
                    writer.Flush();
                    memoryStream.Position = 0;

                    using (StreamReader reader = new StreamReader(memoryStream, encoding ?? Encoding.Default))
                    {
                        content = reader.ReadToEnd();
                        writer.Close();
                        reader.Close();
                        memoryStream.Close();
                    }
                }
            }

            return content;
        }

        public void WriteCsv(DataTable dataTable, string filePath)
        {
            WriteCsv(dataTable, filePath, null);
        }
        public void WriteCsv(DataTable dataTable, string filePath, Encoding encoding)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, false, encoding ?? Encoding.Default))
            {
                WriteToStream(dataTable, writer);
                writer.Flush();
                writer.Close();
            }
        }
        public void WriteCsv(DataTable dataTable, Stream stream)
        {
            WriteCsv(dataTable, stream, null);
        }
        public void WriteCsv(DataTable dataTable, Stream stream, Encoding encoding)
        {
            stream.Position = 0;
            _streamWriter = new StreamWriter(stream, encoding ?? Encoding.Default);
            WriteToStream(dataTable, _streamWriter);
            _streamWriter.Flush();
            stream.Position = 0;
        }

        public string WriteCsv(DataTable dataTable, Encoding encoding)
        {
            string content = string.Empty;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(memoryStream, encoding ?? Encoding.Default))
                {
                    WriteToStream(dataTable, writer);
                    writer.Flush();
                    memoryStream.Position = 0;

                    using (StreamReader reader = new StreamReader(memoryStream, encoding ?? Encoding.Default))
                    {
                        content = reader.ReadToEnd();
                        writer.Close();
                        reader.Close();
                        memoryStream.Close();
                    }
                }
            }

            return content;
        }
        private void WriteToStream(CsvFile csvFile, TextWriter writer)
        {
            if (csvFile.Headers.Count > 0)
                WriteRecord(csvFile.Headers, writer);

            csvFile.Records.ForEach(record => WriteRecord(record.Fields, writer));
        }
        private void WriteToStream(DataTable dataTable, TextWriter writer)
        {
            List<string> fields = (from DataColumn column in dataTable.Columns select column.ColumnName).ToList();
            WriteRecord(fields, writer);

            foreach (DataRow row in dataTable.Rows)
            {
                fields.Clear();
                fields.AddRange(row.ItemArray.Select(o => o.ToString()));
                WriteRecord(fields, writer);
            }
        }
        private void WriteRecord(IList<string> fields, TextWriter writer)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                bool quotesRequired = fields[i].Contains(",");
                bool escapeQuotes = fields[i].Contains("\"");
                bool returnKey = fields[i].Contains("\n");
                var fieldValue = ((escapeQuotes || returnKey) ? fields[i].Replace("\"", "\"\"") : fields[i]);

                if (ReplaceCarriageReturnsAndLineFeedsFromFieldValues && (fieldValue.Contains("\r") || fieldValue.Contains("\n")))
                {
                    quotesRequired = true;
                    fieldValue = fieldValue.Replace("\r\n", CarriageReturnAndLineFeedReplacement);
                }

                writer.Write(string.Format("{0}{1}{0}{2}",
                    (quotesRequired || escapeQuotes || returnKey ? "\"" : string.Empty),
                    fieldValue,
                    (i < (fields.Count - 1) ? "," : string.Empty)));
            }

            writer.WriteLine();
        }

        public void Dispose()
        {
            if (_streamWriter == null)
                return;

            _streamWriter.Close();
            _streamWriter.Dispose();
        }
    }
}
