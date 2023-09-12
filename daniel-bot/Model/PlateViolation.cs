using System.Text.Json.Serialization;

namespace daniel_bot.Model
{
    public class PlateViolation
    {
        [JsonPropertyName("summons_number")]
        public string SummonsNumber { get; set; }

        [JsonPropertyName("plate")]
        public string PlateId { get; set; }

        [JsonPropertyName("state")]
        public string RegistrationState { get; set; }

        [JsonPropertyName("license_type")]
        public string PlateType { get; set; }

        [JsonPropertyName("issue_date")]
        public string IssueDate { get; set; }

        [JsonPropertyName("violation")]
        public string Violation { get; set; }

        [JsonPropertyName("fine_amount")]
        public string FineAmount { get; set; }

        [JsonPropertyName("penalty_amount")]
        public string PenaltyAmount { get; set; }

        [JsonPropertyName("interest_amount")]
        public string InterestAmount { get; set; }

        [JsonPropertyName("reduction_amount")]
        public string ReductionAmount { get; set; }

        [JsonPropertyName("payment_amount")]
        public string PaymentAmount { get; set; }

        [JsonPropertyName("amount_due")]
        public string AmountDue { get; set; }

        [JsonPropertyName("issuing_agency")]
        public string IssueAgency { get; set; }

        [JsonPropertyName("violation_time")]
        public string ViolationTime { get; set; }

        [JsonPropertyName("violation_status")]
        public string ViolationStatus { get; set; }

        [JsonPropertyName("summons_image")]
        public SummonsImage SummonsImage { get; set; }
    }
}