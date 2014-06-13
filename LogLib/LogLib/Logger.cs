using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using UnityEngine; 

namespace LogLib
{
    class LogEntry<T>
    {
        public String name;

        public LogEntry(String name)
        {
            this.name = name;
        }

        public void Log(DataRow row, T value)
        {
            if (row == null)
            {
                throw new System.InvalidOperationException("DataTable has no row to log");
            }
            else
            {
                try
                {
                    row[name] = value;
                }
                catch
                {
                    throw new System.InvalidOperationException("DataTable cannot write " + value + " to " + name);
                }
            }
        }
    }

    public class Logger<T>
    {
        public const String ESC = ";";

        private DataTable table;
        private DataRow currentRow;
        private String user;
        private String condition;
        private LogEntry<T> measure;
        private Dictionary<String, LogEntry<String>> factors;

        public Logger(String measure, String user, String condition)
        {
            table = new DataTable(user);
            this.measure = new LogEntry<T>(measure);
            this.factors = new Dictionary<String, LogEntry<String>>();
            this.user = user;
            this.condition = condition;

            table.Columns.Add("userID", typeof(String));
            table.Columns.Add("condition", typeof(String));
            table.Columns.Add(measure, typeof(T));

            currentRow = null;
        }

        public void AddFactor(String name)
        {
            this.factors.Add(name, new LogEntry<String>(name));
            table.Columns.Add(name, typeof(String));
        }

        public void NewEntry()
        {
            currentRow = table.NewRow();
            table.Rows.Add(currentRow);
            currentRow["userID"] = user;
            currentRow["condition"] = condition;
        }

        public void Log(String factor, String value)
        {
            try
            {
                LogEntry<String> entry = this.factors[factor];
                entry.Log(currentRow, value);
            }
            catch
            {
                throw new System.IndexOutOfRangeException("No factor " + factor + " registered in this table");
            }
        }

        public void Log(T measureValue)
        {
            this.measure.Log(currentRow, measureValue);
        }

        public DataRow[] GetDataSingleRow()
        {
            StringBuilder sb = new StringBuilder();
            String sort = "";
            DataRow[] result = null;

            int counter = 0;
            int numElements = factors.Count;
            foreach (KeyValuePair<String, LogEntry<String>> factor in factors)
            {
                sb.Append(factor.Value.name);

                if (counter == numElements - 1)
                {
                    sb.Append(" ASC");
                }
                else
                {
                    sb.Append(", ");
                }
                counter++;
            }
            sort = sb.ToString();

            result = table.Select("userID = '" + this.user + "'", sort);

            return result;
        }

        public void WriteSingleRowCSV(StreamWriter writer, bool writeHeader)
        {
            DataRow[] rows = GetDataSingleRow();
            writer.AutoFlush = true;

            if (writeHeader)
                WriteColumnsCSV(writer, rows);

            StringBuilder sb = new StringBuilder();
            sb.Append(this.user);
            sb.Append(ESC);
            sb.Append(this.condition);
            for (int i = 0; i < rows.Length; i++)
            {
                sb.Append(ESC);
                sb.Append(rows[i][2]);
            }
            String line = sb.ToString();
            writer.WriteLine(line);
        }

        private void WriteColumnsCSV(StreamWriter writer, DataRow[] rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("userID");
            sb.Append(ESC);
            sb.Append("condition");
            for (int i = 0; i < rows.Length; i++)
            {
                int factorCounter = 0;
                sb.Append(ESC);
                bool firstFactor = true;
                foreach (KeyValuePair<String, LogEntry<String>> factor in factors)
                {
                    if (!firstFactor)
                        sb.Append("--");
                    sb.Append(factor.Key);
                    sb.Append("_");
                    sb.Append(rows[i][3 + factorCounter]);
                    firstFactor = false;
                    factorCounter++;
                }
            }

            String line = sb.ToString();
            writer.AutoFlush = true;
            writer.WriteLine(line);
        }
    }
}
