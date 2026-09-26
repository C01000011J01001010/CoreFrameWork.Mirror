using System.Collections.Generic;
using System.Text;

namespace CoreEditor.Helpers
{
    public static class CsvParserHelper
    {
        // 쉼표와 큰따옴표 규칙을 준수하여 텍스트 한 줄을 컬럼 배열로 분리합니다
        public static string[] ParseLine(string line)
        {
            List<string> result = new List<string>();
            StringBuilder currentToken = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\"')
                {
                    // 큰따옴표 내부에서 큰따옴표 두 개가 연속으로 오면 하나의 큰따옴표 문자로 처리합니다
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        currentToken.Append('\"');
                        i++;
                    }
                    else
                    {
                        // 문자열을 감싸는 큰따옴표의 시작과 끝을 전환합니다
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // 큰따옴표 외부의 쉼표는 컬럼 구분자로 인식하여 배열에 추가합니다
                    result.Add(currentToken.ToString());
                    currentToken.Clear();
                }
                else
                {
                    currentToken.Append(c);
                }
            }

            result.Add(currentToken.ToString());
            return result.ToArray();
        }
    }
}