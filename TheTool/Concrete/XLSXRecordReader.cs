using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using XLSXTools;
using System.Globalization;

namespace TheTool
{
    public class XLSXRecordReader : RecordReader
    {
        private string filePath;
        private XLSXRowReader reader;

        public XLSXRecordReader(string filePath, bool customRangeCalculation = true, int rowStart = 0) : this(filePath, "Sheet1", customRangeCalculation, rowStart)
        {

        }

        public XLSXRecordReader(string filePath, string sheetName, bool customRangeCalculation = true, int rowStart = 0)
        {
            reader = new XLSXRowReader(filePath, sheetName, customRangeCalculation, rowStart);
        }

        public void Close()
        {
            reader.Close();
        }

        public bool ReadNextRecord(out string[] record)
        {
            List<string> resultRecord = new List<string>();
            record = null;

            Cell[] cells;
            bool result = reader.ReadNextCells(out cells);
            if (!result) return result;
            else
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    resultRecord.Add(FormatCellValue(cells[i]));
                }
                record = resultRecord.ToArray();
            }

            return result;
        }

        private string FormatCellValue(Cell cell)
        {
            string cellValue = reader.GetCellValue(cell);
            string cellFormat = cell != null ? reader.GetCellFormat(cell) : null;

            if (cell == null || (cell != null && cell.DataType != null && cell.DataType == CellValues.SharedString) || (cellFormat != null && cellFormat == "@"))
            {
                return cellValue;
            }
            else
            {
                decimal number;
                bool isNumber = decimal.TryParse(cellValue, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

                if (isNumber)
                {
                    if (cellFormat != null)
                    {
                        bool isAccounting = cellFormat.IndexOf('#') >= 0;
                        if (isAccounting)
                        {
                            number = decimal.Round(number, 2);
                            return number.ToString("#.##");
                        }
                        else
                        {
                            number = decimal.Round(number, 9);
                            return number.ToString("#.#########");
                        }
                    }
                    else
                    {
                        number = decimal.Round(number, 9);
                        return number.ToString("#.#########");
                    }
                }
                else return cellValue;
            }
        }
    }
}
