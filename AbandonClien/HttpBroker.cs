using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Specialized;

namespace AbandonClien
{
    class HttpBroker
    {
        protected static CookieContainer Cookies;
        protected static HttpClientHandler Handler;
        protected static HttpClient Client;

        public enum Method
        {
            Get,
            Post
        };

        public HttpBroker()
        {
            Cookies = new CookieContainer();
            Handler = new HttpClientHandler();
            Handler.CookieContainer = Cookies;

            Client = new HttpClient(Handler);
        }

        /// <summary>
        /// 웹 페이지에서 데이터를 가져온다. 만약에 서버 오류로 실패할경우 자동으로 재시도한다.
        /// </summary>
        /// <param name="url">데이터를 가져올 URL</param>
        /// <param name="param">GET, POST 데이터</param>
        /// <param name="method">HTTP 요청 method</param>
        /// <returns></returns>
        public async Task<string> FetchPage(string url, Dictionary<string, string> parameters = null, Method method = Method.Get, bool clearFile = false)
        {
            // GET은 url 뒤에 문자열을 붙인다.
            if (method == Method.Get)
            {
                if (parameters != null)
                {
                    NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

                    foreach (KeyValuePair<string, string> parameter in parameters)
                    {
                        queryString.Add(parameter.Key, parameter.Value);
                    }

                    url = url + "?" + queryString.ToString();
                }


                while (true)
                {
                    try
                    {
                        HttpResponseMessage response = await Client.GetAsync(url);
                        return await response.Content.ReadAsStringAsync();
                    }
                    catch (System.AggregateException)
                    {
                        Console.WriteLine("통신중 오류 발생.. 5초뒤 다시 시도합니다.");
                        System.Threading.Thread.Sleep(5000);
                    }
                }
            }
            else /*if  ( method == Method.Post) */ // POST는 HttpContent로 보낸다.
            {
                HttpContent content;
                
                // 파일 업로드가 있는 경우 multipart로 전환하고 blank.png를 업로드해서 기존파일을 지운다.
                if (clearFile == true)
                {
                    var multipartContent = new MultipartFormDataContent();
                    foreach (KeyValuePair<string, string> parameter in parameters)
                    {
                        multipartContent.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(parameter.Value)), parameter.Key);
                    }
                    multipartContent.Add(new ByteArrayContent(Convert.FromBase64String("R0lGODlhAQABAIAAAAUEBAAAACwAAAAAAQABAAACAkQBADs=")), "bf_file[]", "blank.png");

                    content = multipartContent;
                }
                else
                {
                    content = new FormUrlEncodedContent(parameters);
                }
                while (true)
                {
                    try
                    {
                        HttpResponseMessage response = await Client.PostAsync(url, content);
                        return await response.Content.ReadAsStringAsync();
                    }
                    catch (System.AggregateException)
                    {
                        Console.WriteLine("통신중 오류 발생.. 5초뒤 다시 시도합니다.");
                        System.Threading.Thread.Sleep(5000);
                    }
                }
            }
        }


    }
}
