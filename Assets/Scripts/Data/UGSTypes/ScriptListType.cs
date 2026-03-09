using System.Collections.Generic;
using GoogleSheet.Type;

namespace GoogleSheet.Type
{
    // 쉼표가 포함될 수 있는 텍스트 리스트용 타입
    // 시트 컬럼 타입: scriptlist
    // 셀 포맷 예시: [열심히 공부했다|졸면서 공부했다|집중하며 공부했다]
    [Type(Type: typeof(List<string>), TypeName: new string[] { "scriptlist", "ScriptList" })]
    public class ScriptListType : IType
    {
        public object DefaultValue => new List<string>();

        public object Read(string value)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(value) || value == "[]")
                return result;

            string inner = value;
            if (value[0] == '[' && value[value.Length - 1] == ']')
                inner = value.Substring(1, value.Length - 2);

            // 파이프(|)가 있으면 스크립트 리스트, 없으면 쉼표(,)로 분리
            char separator = inner.Contains('|') ? '|' : ',';
            var parts = inner.Split(separator);
            foreach (var part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                    result.Add(part);
            }
            return result;
        }

        public string Write(object value)
        {
            var list = (List<string>)value;
            if (list == null || list.Count == 0) return "[]";
            return "[" + string.Join("|", list) + "]";
        }
    }
}
