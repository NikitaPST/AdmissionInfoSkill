using System;
using System.Collections.Generic;
using System.Text;

namespace AdmissionInfoLambda
{
    /// <summary>
    /// Skill resource for some locale.
    /// </summary>
    class SkillResource
    {
        public string Language { get; set; }
        public string SkillName { get; set; }
        public string WelcomeMessage { get; set; }
        public string ShutdownMessage { get; set; }
        public string HelpMessage { get; set; }
        public string StartOverMessage { get; set; }
        public string ApplicationFeeMessage { get; set; }
        public string TuitionMessage { get; set; }
        public string FinancialAidMessage { get; set; }
        public string AdmissionRateMessage { get; set; }
        public string NoUniversityFound { get; set; }
        public string NoApplicationFeeFound { get; set; }
        public string NoTuitionFound { get; set; }
        public string NoFinancialAidFound { get; set; }
        public string NoAdmissionRateFound { get; set; }

        public SkillResource(string language)
        {
            Language = language;
        }
    }
}
