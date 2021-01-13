using Microsoft.BotBuilderSamples.Bots;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace AlexPicBot.Translator
{
    public class Sentiments
    {
        string SentimentUri;
        string SubscriptionKey;

        public Sentiments(string uri, string subscriptionKey)
        {
            SentimentUri = uri;
            SubscriptionKey = subscriptionKey;
        }

        public Decimal Detect(string input)
        {
            try
            {
                return ReadRespons(SentimentUri,input);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

 
        private decimal ReadRespons(string uri, string text)
        {
            WebRequest sentimentWebRequest = WebRequest.CreateHttp(uri);
            sentimentWebRequest.Method = "POST";
            sentimentWebRequest.Headers.Add("Content-Type", "application/json");            
            sentimentWebRequest.Headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);

            using (var streamWriter = new StreamWriter(sentimentWebRequest.GetRequestStream()))
            {
                string json = "{\"documents\": [ { \"language\": \"en\",\"id\": 1,\"text\": \"" + text + "\" } ]}";

                streamWriter.Write(json);
            }
//{'documents':[{'id':'1','score':0.83896893262863159}],'errors':[]}

            using (WebResponse response = sentimentWebRequest.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    Encoding encode = Encoding.GetEncoding("UTF-8");
                    using (StreamReader translatedStream = new StreamReader(stream, encode))
                    {
                        string respond = translatedStream.ReadToEnd();
                        Regex r = new Regex(@"score.*(?<score>0\.\d{5})");
                        var g = r.Match(respond);
                        if (g.Success)
                            return Convert.ToDecimal(g.Groups["score"].Value.Trim());
                        else
                            return 0.5M;
                    }
                }
            }
        }
    }
}
