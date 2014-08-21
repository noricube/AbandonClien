using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using System.Web;

namespace AbandonClien
{
    /// <summary>
    /// clien과 통신하는 클래스
    /// Login 함수를 제외하고 로그인 해야 사용가능하고 조회중에 로그아웃이 된 경우 자동 로그인을 시도한다.
    /// </summary>
    class Clien
    {
        protected HttpBroker Broker;

        protected string Username { get; set; }
        protected string Password { get; set; }

        public Clien(string username, string password)
        {
            Broker = new HttpBroker();

            Username = username;
            Password = password;
        }

        public async Task<bool> Login()
        {
            var postData = new Dictionary<string, string>();
            postData.Add("mb_id", Username);
            postData.Add("mb_password", Password);

            string response = await Broker.FetchPage("https://www.clien.net/cs2/bbs/login_check.php", postData, HttpBroker.Method.Post);
            // 로그인 성공시 nowlogin=1로 기존페이지로 이동하게 한다 
            if ( response.IndexOf("nowlogin=1") >= 0 )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<List<ArticleInfo>> GetMyArticles(int page = 1)
        {
            Console.WriteLine("나의 글 목록 " + page + "페이지를 가져옵니다.");

            var articles = new List<ArticleInfo>();
            
            var queryData = new Dictionary<string, string>();
            queryData.Add("page", page.ToString());

            string content = await Broker.FetchPage("http://www.clien.net/cs2/modules/my_comment.php", queryData);

            var html = new HtmlDocument();
            html.LoadHtml(content);

            var document = html.DocumentNode;

            int lastPage = 1;

            // 모든 글 선택
            foreach (var node in document.QuerySelectorAll(".board_main td a"))
            {
                var hrefAttr = node.Attributes["href"];


                int queryIndex = hrefAttr.Value.IndexOf("?");

                // ?? 예상하지 못한 링크. 여기에는 게시판 주소 링크와 페이지 링크만 와야함
                if (queryIndex == -1)
                {
                    continue;
                }

                var hrefQuery = HttpUtility.ParseQueryString(hrefAttr.Value.Substring(queryIndex + 1));

                // page링크가 아닌경우 - 게시물 링크
                // ../bbs/board.php?bo_table=park&wr_id=31130081
                if (hrefQuery["page"] == null)
                {
                    articles.Add(new ArticleInfo()
                    {
                        ID = long.Parse(hrefQuery["wr_id"]),
                        Table = hrefQuery["bo_table"],
                        Subject = node.InnerText
                    });
                }
                else // 페이지 번호 ?&page=3
                {
                    // 첫페이지일 경우만 페이지를 파싱하게 한다.
                    if (page == 1)
                    {
                        // 논리적으로 계속 덮어쓰지만. 마지막 페이지 번호가 글 순서상 제일 마지막이기떄문에
                        // 최종 결과가 된다.
                        lastPage = int.Parse(hrefQuery["page"]);
                    }
                }

            }

            for (int i = 2; i <= lastPage; i++)
            {
                // 혹시모르니 각각 요청사이에 잠깐 쉰다.
                await Task.Delay(1000);

                articles.AddRange(await GetMyArticles(i));
            }

            return articles;
        }

        public async Task<List<CommentInfo>> GetMyCommentsInArticle(ArticleInfo article, int comment_page = 1)
        {
            var comments = new List<CommentInfo>();

            var queryData = new Dictionary<string, string>();
            queryData.Add("bo_table", article.Table);
            queryData.Add("wr_id", article.ID.ToString());
            queryData.Add("comment_page", comment_page.ToString());

            string content = await Broker.FetchPage("http://www.clien.net/cs2/bbs/board.php", queryData);
            
            var html = new HtmlDocument();
            html.LoadHtml(content);

            var document = html.DocumentNode;

            // 모든 글 선택
            foreach (var node in document.QuerySelectorAll("img[alt='삭제']"))
            {
                // javascript:comment_delete('./delete_comment.php?bo_table=park&amp;comment_id=31255959&amp;cwin=&amp;comment_page=1&amp;page=');
                string deleteScript = node.ParentNode.Attributes["href"].Value;

                int queryIndex = deleteScript.IndexOf('?');
                // ? 문자열 시작부터  "');" 전까지 substring
                string deleteQuery = deleteScript.Substring(queryIndex + 1, deleteScript.Length - 4 - queryIndex);
                var parseDeleteQuery = HttpUtility.ParseQueryString(deleteQuery);

                comments.Add(new CommentInfo()
                {
                    ArticleID = article.ID,
                    Table = article.Table,
                    CommentID = long.Parse(parseDeleteQuery["comment_id"])
                });
            }

            return comments;
        }

        public async Task<bool> UpdateComment(CommentInfo comment, string message)
        {
            var postData = new Dictionary<string, string>();
            postData.Add("w", "cu");
            postData.Add("bo_table", comment.Table);
            postData.Add("wr_id", comment.ArticleID.ToString());
            postData.Add("comment_id", comment.CommentID.ToString());
            postData.Add("wr_content", message);
            
            string response = await Broker.FetchPage("http://www.clien.net/cs2/bbs/write_comment_update.php", postData, HttpBroker.Method.Post);
            
            // 글에서 다시 코멘트 번호로 이동하는것 변경(업데이트 성공)
            if ( response.IndexOf("#c_" + comment.CommentID.ToString()) > 0 )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> DeleteComment(CommentInfo comment)
        {
            var queryData = new Dictionary<string, string>();
            queryData.Add("bo_table", comment.Table);
            queryData.Add("comment_id", comment.CommentID.ToString());
            
            string response = await Broker.FetchPage("http://www.clien.net/cs2/bbs/delete_comment.php", queryData, HttpBroker.Method.Get);

            Console.WriteLine(response);

            return true;
            
            // 글에서 글번호를 발견하면 성공
            if ( response.IndexOf("wr_id=" + comment.ArticleID) > 0 )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
