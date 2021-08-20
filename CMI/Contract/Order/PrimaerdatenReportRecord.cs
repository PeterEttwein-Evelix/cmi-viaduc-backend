using System;

namespace CMI.Contract.Order
{
    public class PrimaerdatenReportRecord
    {
        public int? OrderId { get; set; }
        public int?  VeId { get; set; }
        public float Size{ get; set; }
        public int FileCount { get; set; }
        public string Source { get; set; }
        public DateTime? NeuEingegangen { get; set; }
        public DateTime? FreigabePrüfen { get; set; }
        public DateTime? FürDigitalisierungBereit { get; set; }
        public DateTime? FürAushebungBereit { get; set; }
        public DateTime? Ausgeliehen { get; set; }
        public DateTime? ZumReponierenBereit { get; set; }
        public DateTime? PrimaryDataLinkCreationDate { get; set; }
        public DateTime? StartFirstSynchronizationAttempt { get; set; }
        public DateTime? StartLastSynchronizationAttempt { get; set; }
        public DateTime? CompletionLastSynchronizationAttempt { get; set; }
        public int CountSynchronizationAttempts { get; set; }
        public DateTime? ClickButtonPrepareDigitalCopy { get; set; }
        public TimeSpan? EstimatedPreparationTimeVeAccordingDetailPage { get; set; }
        public DateTime? StartFirstPreparationAttempt { get; set; }
        public DateTime? StartLastPreparationAttempt { get; set; }
        public DateTime? CompletionLastPreparationAttempt { get; set; }
        public int? CountPreparationAttempts { get; set; }
        public DateTime? StorageUseCopyCache { get; set; }
        public DateTime? ShippingMailReadForDownload  { get; set; }
    }
}
