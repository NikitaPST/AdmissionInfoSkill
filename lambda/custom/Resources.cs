using System;
using System.Collections.Generic;

namespace AdmissionInfoLambda
{
    /// <summary>
    /// Contains all available resources for all supported locales.
    /// </summary>
    class Resources
    {
        const string GB_LOCALE = "en-GB";
        const string US_LOCALE = "en-US";

        /// <summary>
        /// Retrieve resources for specified locale.
        /// </summary>
        /// <param name="locale">Locale.</param>
        /// <returns>Resources for specified locale.</returns>
        public SkillResource GetResources(string locale)
        {
            SkillResource resources = null;
            
            switch (locale)
            {
                case GB_LOCALE:
                case US_LOCALE:
                default:
                    resources = GetENResource(locale);
                    break;
            }
            return resources;
        }

        /// <summary>
        /// Generates resources for english locale.
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        SkillResource GetENResource(string locale)
        {
            SkillResource resource = new SkillResource(locale);

            resource.SkillName = "Admission info";
            resource.WelcomeMessage = "Welcome to " + resource.SkillName + ". Learn about US colleges tuition, application fees, average financial aid packages and admission rates.";
            resource.ShutdownMessage = "Ok.";
            resource.HelpMessage = "I can help you find basic information about US college admission. For example, tuition cost, application fees, financial aid packages and admission rates.";
            resource.StartOverMessage = "Ok, starting over.";
            resource.ApplicationFeeMessage = "Application fee for {0} is {1}.";
            resource.TuitionMessage = "Tuition cost for {0} is {1}.";
            resource.FinancialAidMessage = "Average financial aid package in {0} is {1}.";
            resource.AdmissionRateMessage = "Admission rate in {0} is {1}.";

            resource.NoUniversityFound = "Hmm. I couldn't find data for {0}.";
            resource.NoApplicationFeeFound = "No information available for application fees in {0}.";
            resource.NoTuitionFound = "No information available for tuition cost in {0}.";
            resource.NoFinancialAidFound = "No information available for financial aid packages in {0}.";
            resource.NoAdmissionRateFound = "No information available for admission rates in {0}.";

            return resource;
        }
    }
}
