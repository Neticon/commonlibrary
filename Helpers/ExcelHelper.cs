using ClosedXML.Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.Helpers
{
    public static class ExcelHelper
    {
        public static void WriteSheetFromKey(XLWorkbook workbook, string sheetName, JArray rows, string key)
        {
            var objects = rows.Select(r => r[key] as JObject).Where(o => o != null).ToList();
            if (objects.Count == 0) return;

            var headers = objects[0]!.Properties().Select(p => p.Name).ToList();
            WriteDataSheet(workbook, sheetName, headers,
                objects.Select(o => headers.Select(h => (object?)o![h])));
        }

        public static void WriteSheetFromNestedArray(XLWorkbook workbook, string sheetName, JArray rows, string key)
        {
            var allItems = rows
                .SelectMany(r => (r[key] as JArray ?? new JArray()).OfType<JObject>())
                .ToList();

            if (allItems.Count == 0) return;

            var headers = allItems[0].Properties().Select(p => p.Name).ToList();
            WriteDataSheet(workbook, sheetName, headers,
                allItems.Select(o => headers.Select(h => (object?)o[h])));
        }

        public static void WriteDataSheet(
            XLWorkbook workbook,
            string sheetName,
            IReadOnlyList<string> headers,
            IEnumerable<IEnumerable<object?>> rows)
        {
            var ws = workbook.Worksheets.Add(sheetName);

            for (int i = 0; i < headers.Count; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            int rowNum = 2;
            foreach (var row in rows)
            {
                int col = 1;
                foreach (var value in row)
                    SetCellValue(ws.Cell(rowNum, col++), value);
                rowNum++;
            }

            ws.Columns().AdjustToContents();
        }

        public static void SetCellValue(IXLCell cell, object? value)
        {
            if (value is null) return;

            // Unwrap JToken first so the type-specific cases below handle everything uniformly
            if (value is JToken token)
            {
                SetJTokenValue(cell, token);
                return;
            }

            if (value is bool b)
                cell.Value = b;
            else if (value is long l)
                cell.Value = (double)l;
            else if (value is int i)
                cell.Value = (double)i;
            else if (value is double d)
                cell.Value = d;
            else if (value is float f)
                cell.Value = (double)f;
            else if (value is decimal dec)
                cell.Value = (double)dec;
            else
                cell.Value = value.ToString() ?? string.Empty;
        }

        private static void SetJTokenValue(IXLCell cell, JToken token)
        {
            if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) return;

            switch (token.Type)
            {
                case JTokenType.Boolean:
                    cell.Value = token.Value<bool>();
                    break;
                case JTokenType.Integer:
                    cell.Value = (double)token.Value<long>();
                    break;
                case JTokenType.Float:
                    cell.Value = token.Value<double>();
                    break;
                case JTokenType.Object:
                case JTokenType.Array:
                    cell.Value = token.ToString(Formatting.None);
                    break;
                default:
                    cell.Value = token.ToString() ?? string.Empty;
                    break;
            }
        }
    }
}
