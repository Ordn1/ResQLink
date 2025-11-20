using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Relief_Goods_Categories")]
public class ReliefGoodCategory
{
    public int RgId { get; set; }
    public ReliefGood ReliefGood { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
