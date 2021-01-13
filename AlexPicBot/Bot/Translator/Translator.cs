using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class Translator
    {
        string token;
        string TranslatorUri;
        string CognitiveServicesTokenUri;
        string SubscriptionKey;

        public Translator(string translatorUri, string cognitiveServicesTokenUri, string subscriptionKey)
        {
            TranslatorUri = translatorUri;
            CognitiveServicesTokenUri = cognitiveServicesTokenUri;
            SubscriptionKey = subscriptionKey;
            token = Task.Run(GetBearerTokenForTranslator).Result;
        }

        internal async Task<string> GetBearerTokenForTranslator()
        {
            var azureSubscriptionKey = SubscriptionKey;
            var azureAuthToken = new APIToken(azureSubscriptionKey, CognitiveServicesTokenUri);
            return await azureAuthToken.GetAccessTokenAsync();
        }

        public string Translate(string input, string inputLang, string outputLang)
        {
            try
            {
                var test = ReadRespons($"{TranslatorUri}Translate?text={HttpUtility.UrlEncode(input)}&from={inputLang}&to={outputLang}");
                return test;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal string Detect(string input)
        {
            try
            {
                return ReadRespons($"{TranslatorUri}Detect?text=" + HttpUtility.UrlEncode(input));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        private string ReadRespons(string uri)
        {
            WebRequest translationWebRequest = WebRequest.Create(uri);
            translationWebRequest.Headers.Add("Authorization", token);

            using (WebResponse response = translationWebRequest.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    Encoding encode = Encoding.GetEncoding("UTF-8");
                    using (StreamReader translatedStream = new StreamReader(stream, encode))
                    {
                        XmlDocument xTranslation = new XmlDocument();
                        xTranslation.LoadXml(translatedStream.ReadToEnd());
                        return xTranslation.InnerText;
                    }
                }
            }
        }
    }
}