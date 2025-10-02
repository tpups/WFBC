using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    public class Standings
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        public DateTime? CreatedAt { get; set; }

        [Required]
        public DateTime? LastUpdatedAt { get; set; }

        public string? Year { get; set; }

        public DateTime Date { get; set; }

        // Team identification
        public string? TeamId { get; set; }
        public string? Manager { get; set; }
        public string? TeamName { get; set; }

        // Hitting categories (raw values)
        public decimal AVG { get; set; }
        public decimal OPS { get; set; }
        public int R { get; set; }
        public int RBI { get; set; }
        public int SB { get; set; }
        public int HR { get; set; }

        // Pitching categories (raw values)
        public decimal ERA { get; set; }
        public decimal WHIP { get; set; }
        public int K { get; set; }
        public decimal IP { get; set; }
        public int QS { get; set; }
        public int S { get; set; } // Saves

        // Points per category
        public decimal AVG_Points { get; set; }
        public decimal OPS_Points { get; set; }
        public decimal R_Points { get; set; }
        public decimal RBI_Points { get; set; }
        public decimal SB_Points { get; set; }
        public decimal HR_Points { get; set; }
        public decimal ERA_Points { get; set; }
        public decimal WHIP_Points { get; set; }
        public decimal K_Points { get; set; }
        public decimal IP_Points { get; set; }
        public decimal QS_Points { get; set; }
        public decimal S_Points { get; set; }

        // Totals
        public decimal TotalHittingPoints { get; set; }
        public decimal TotalPitchingPoints { get; set; }
        public decimal TotalPoints { get; set; }
        public int OverallRank { get; set; }
        public int HittingRank { get; set; }
        public int PitchingRank { get; set; }
    }
}
