using System.Collections.Generic;
using GoogleSheet.Type;

namespace GoogleSheet.Type
{
    // 시트 컬럼 타입: shopitems
    // 셀 포맷 예시: (item_book:1500,item_pen:500)
    [Type(Type: typeof(List<ShopItemEntry>), TypeName: new string[] { "shopitems", "ShopItems" })]
    public class ShopItemListType : IType
    {
        public object DefaultValue => new List<ShopItemEntry>();

        public object Read(string value)
        {
            var result = new List<ShopItemEntry>();
            if (string.IsNullOrEmpty(value) || value == "()")
                return result;

            var entries = ReadUtil.GetParenthesisValueToArray(value);
            if (entries == null) return result;

            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                if (parts.Length != 2) continue;

                result.Add(new ShopItemEntry
                {
                    itemId = parts[0].Trim(),
                    price = int.Parse(parts[1].Trim())
                });
            }
            return result;
        }

        public string Write(object value)
        {
            var list = (List<ShopItemEntry>)value;
            if (list == null || list.Count == 0) return "()";

            var parts = new List<string>();
            foreach (var entry in list)
                parts.Add($"{entry.itemId}:{entry.price}");

            return "(" + string.Join(",", parts) + ")";
        }
    }
}
