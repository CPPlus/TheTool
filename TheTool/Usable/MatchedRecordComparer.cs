using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XLSXTools;

namespace TheTool
{
    public class MatchedRecordComparer
    {
        private DiscrepancyOutputter outputter;
        private const int ID_COLUMN_INDEX = 0;

        private const int FIRST_RECORD_IS_NULL = 0;
        private const int SECOND_RECORD_IS_NULL = 1;
        private const int NO_NULL_RECORDS = -1;

        private RoundingSettings settings;
        private int[] ignoredColumnIndices;

        public MatchedRecordComparer(ComparisonData data, RoundingSettings settings, DiscrepancyOutputter outputter)
        {
            List<int> list = new List<int>();
            foreach (string ignoredColumn in data.ignoredColumns)
            {
                list.Add(XLSXUtils.CellReferenceToColumnIndex(ignoredColumn));
            }
            ignoredColumnIndices = list.ToArray();

            this.settings = settings;
            this.outputter = outputter;
        }

        public DiscrepancyType Compare(string[] first, string[] second)
        {
            int nullRecordIndex = GetNullRecordIndex(first, second);
            if (nullRecordIndex == NO_NULL_RECORDS)
            {
                return RecordsAreEqual(first, second);
            } else
            {
                outputter.MissingRecord(nullRecordIndex == FIRST_RECORD_IS_NULL ? second : first, nullRecordIndex);
                return DiscrepancyType.DISCREPANCY;
            }
        }

        private bool ColumnIsIgnored(int index)
        {
            foreach (int i in ignoredColumnIndices)
                if (i == index)
                    return true;

            return false;
        }

        private DiscrepancyType RecordsAreEqual(string[] first, string[] second)
        {
            DiscrepancyType result = DiscrepancyType.NONE;
            string[] comparedRecord = new string[first.Length];
            for (int i = 0; i < first.Length; i++)
            {
                string value1 = first[i];
                string value2 = second[i];

                string value1Formatted = string.Empty, 
                       value2Formatted = string.Empty;

                bool columnIsIgnored = ColumnIsIgnored(i);
                DiscrepancyType type;
                if (columnIsIgnored)
                    type = DiscrepancyType.NONE;
                else
                    type = ValuesAreEqual(value1, value2, out value1Formatted, out value2Formatted);
                
                if (type == DiscrepancyType.DISCREPANCY)
                    comparedRecord[i] = string.Format("'{0}|{1}'", value1Formatted, value2Formatted);
                else if (type == DiscrepancyType.ROUNDING)
                    comparedRecord[i] = string.Format("'{0}||{1}'", value1Formatted, value2Formatted);
                else
                {
                    if (columnIsIgnored)
                    {
                        comparedRecord[i] = value1;
                    } else
                    {
                        comparedRecord[i] = value1Formatted;
                    }
                    
                }
                    

                if (i == ID_COLUMN_INDEX) type = DiscrepancyType.NONE;

                if (type > result)
                    result = type;
            }
            outputter.Record(comparedRecord, result);

            return result;
        }

        private DiscrepancyType ValuesAreEqual(string value1, string value2, out string value1Formatted, out string value2Formatted)
        {
            DiscrepancyType result;

            decimal value1Decimal, value2Decimal;
            bool value1IsDecimal = DecimalTryParse(value1, out value1Decimal);
            bool value2IsDecimal = DecimalTryParse(value2, out value2Decimal);

            if (value1IsDecimal)
            {
                value1Formatted = FormatDecimalToDigit(value1Decimal, 15);
                value1Formatted = RemoveTrailingZeroes(value1Formatted);
            }
            else value1Formatted = value1;

            if (value2IsDecimal)
            {
                value2Formatted = FormatDecimalToDigit(value2Decimal, 15);
                value2Formatted = RemoveTrailingZeroes(value2Formatted);
            }
            else value2Formatted = value2;

            

            if (value1Formatted.Equals(value2Formatted))
            {
                result = DiscrepancyType.NONE;
            }
            else // Handle discrepancy.
            {
                if (settings.smartRounding)
                {
                    if (value1IsDecimal && value2IsDecimal)
                    {
                        // Within the delta.
                        if (Math.Abs(value1Decimal - value2Decimal) < settings.delta)
                        {
                            // After
                            if (settings.smartRounding && settings.smartRoundingFormatting)
                            {
                                SmartRoundFormat(value1Decimal, value2Decimal, out value1Formatted, out value2Formatted);

                                // Cover special case.
                                if (value1Formatted == "-0") value1Formatted = "0";
                                if (value2Formatted == "-0") value2Formatted = "0";

                                if (value1Formatted.Equals(value2Formatted))
                                    return DiscrepancyType.NONE;
                            }

                            if (settings.smartRounding && settings.smartRoundingFormatting) result = DiscrepancyType.ROUNDING;
                            else result = DiscrepancyType.NONE;

                        } else result = DiscrepancyType.DISCREPANCY;
                    } else result = DiscrepancyType.DISCREPANCY;
                } else result = DiscrepancyType.DISCREPANCY;
            }

            return result;
        }

        private void SmartRoundFormat(decimal number1Dec, decimal number2Dec, out string formattedNumber1, out string formattedNumber2)
        {
            string number1 = FormatDecimalToDigit(number1Dec, 15);
            string number2 = FormatDecimalToDigit(number2Dec, 15);

            string[] number1Split = number1.Split('.');
            string[] number2Split = number2.Split('.');

            // If whole parts are different.
            if (!number1Split[0].Equals(number2Split[0]))
            {
                formattedNumber1 = string.Format("{0}.{1}", number1Split[0], number1Split[1].Substring(0, 1));
                formattedNumber2 = string.Format("{0}.{1}", number2Split[0], number2Split[1].Substring(0, 1));
            } else // Cut to where the first difference is.
            {
                StringBuilder builder1 = new StringBuilder();
                StringBuilder builder2 = new StringBuilder();
                for (int i = 0; i < number1Split[1].Length; i++)
                {
                    builder1.Append(number1Split[1][i]);
                    builder2.Append(number2Split[1][i]);
                    if (!number1Split[1][i].Equals(number2Split[1][i]))
                    {
                        break;
                    } 
                }
                formattedNumber1 = string.Format("{0}.{1}", number1Split[0], builder1.ToString());
                formattedNumber2 = string.Format("{0}.{1}", number2Split[0], builder2.ToString());
            }

            formattedNumber1 = RemoveTrailingZeroes(formattedNumber1);
            formattedNumber2 = RemoveTrailingZeroes(formattedNumber2);
        }

        private string RemoveTrailingZeroes(string number)
        {
            int lastNonZeroIndex = 0;
            for (int i = 0; i < number.Length; i++)
            {
                if (number[i] != '0') lastNonZeroIndex = i;
            }
            number = number.Substring(0, lastNonZeroIndex + 1);

            if (number[number.Length - 1] == '.')
                number = number.Replace(".", string.Empty);

            return number;
        }

        private string FormatDecimalToDigit(decimal number, int digit)
        {
            return decimal.Round(number, digit).ToString("F15");
        }

        private bool DecimalTryParse(string number, out decimal result)
        {
            return decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private int GetNullRecordIndex(string[] first, string[] second)
        {
            string nullValue = string.Empty;
            if (first[0] == nullValue) return FIRST_RECORD_IS_NULL;
            else if (second[0] == nullValue) return SECOND_RECORD_IS_NULL;
            else return NO_NULL_RECORDS;
        }
    }
}
