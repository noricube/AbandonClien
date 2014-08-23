using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xgoogleSharp;
using System.Web;

namespace AbandonClien
{
    class DestroyEverything
    {
        protected Clien Clien;
        protected static List<ArticleInfo> Articles;
        protected static List<CommentInfo> Comments;

        protected Random Rand;
        protected List<string> RandomMessages;
        public DestroyEverything(Clien clien)
        {
            Clien = clien;

            Articles = null;
            Comments = null;

            Rand = new Random();

            RandomMessages = new List<string>();
            RandomMessages.Add("-");
            RandomMessages.Add("삭제");
            RandomMessages.Add("delete");
            RandomMessages.Add("펑");
            RandomMessages.Add("내용삭제");
        }


        public async Task Collect()
        {
            // 우선 내 글목록을 구해옴
            List<ArticleInfo> articles = await Clien.GetMyArticles();

            List<ArticleInfo> searchArticles = await GoogleSearch(articles);
                        

            int totalArticle = articles.Count + searchArticles.Count;
            Console.WriteLine("총 {0}개의 게시물에서 댓글을 검색합니다.", totalArticle);

            List<Task> tasks = new List<Task>();
            var commentBag = new ConcurrentBag<CommentInfo>();

            int cnt = 0;
            // 내가 쓴글에서 코멘트를 검색한다.
            
            foreach (var article in articles)
            {
                Console.WriteLine("[{0}/{1}] {2}", ++cnt, totalArticle, article.Subject);

                Task commentTask = Clien.GetMyCommentsInArticle(article).ContinueWith(x =>
                {
                    foreach (CommentInfo commentInfo in x.Result)
                    {
                        commentBag.Add(commentInfo);
                    }
                });
                tasks.Add(commentTask);

                // 코멘트는 검색은 5개씩 한번에 처리한다.
                if (tasks.Count > 5)
                {
                    await Task.WhenAll(tasks.ToArray());
                    tasks.Clear();
                }
            }

            // 구글에서 찾은글에서 코멘트 검색 
            foreach (var article in searchArticles)
            {
                Console.WriteLine("[{0}/{1}] {2}", ++cnt, totalArticle, article.Subject);

                Task commentTask = Clien.GetMyCommentsInArticle(article).ContinueWith(x =>
                {
                    foreach (CommentInfo commentInfo in x.Result)
                    {
                        commentBag.Add(commentInfo);
                    }
                });
                tasks.Add(commentTask);

                // 코멘트는 검색은 5개씩 한번에 처리한다.
                if (tasks.Count > 5)
                {
                    await Task.WhenAll(tasks.ToArray());
                    tasks.Clear();
                }
            }

            // 남은 코멘트 조회 작업이 완전히 끝날때까지 기다린다.
            await Task.WhenAll(tasks.ToArray());
            tasks.Clear();


            Articles = articles;
            // 댓글은 ConcurrentBag에서 List로 옮긴다.
            Comments = commentBag.ToList<CommentInfo>();

        }

        private async Task<List<ArticleInfo>> GoogleSearch(List<ArticleInfo> myArticles)
        {
            // 구글 검색 키워드를 만든다
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            sb.Append(await Clien.GetMyNickname());
            sb.Append("님\" site:clien.net");

            List<ArticleInfo> searchArticles = new List<ArticleInfo>();

            string keyword = sb.ToString();
            Console.WriteLine("구글에서 {0} 로 검색하여 내가 쓴 댓글이 있는 글도 수집합니다.", keyword);

            GoogleSearch search = new GoogleSearch(keyword, 100);
            while (true)
            {
                var searchResult = search.FetchResults();
                searchResult.Wait();

                foreach (var result in searchResult.Result)
                {
                    // /url?q=http://webcache.googleusercontent.com/search%3Fhl%3Den%26q%3Dcache:phgR10UBmtYJ:http://powoy558vs.egloos.com/2422221%252B%25EB%2585%25B8%25EB%25A6%25AC%25EB%258B%2598%26num%3D100%26%26ct%3Dclnk&amp;sa=U&amp;ei=hzf4U9XsG5S48gW-iIHIDQ&amp;ved=0CKcEECAwZw&amp;usg=AFQjCNEZ3-BmuR67Y2UKzq6nuFHjGOySsQ
                    // "/url?q=" 로 시작하므로 앞에 글자를 제거하자
                    var proxyUrl = HttpUtility.ParseQueryString(HttpUtility.HtmlDecode(result.Url.Substring(4)));
                    var realUrl = HttpUtility.HtmlDecode(proxyUrl["q"]);

                    // 게시판 URL일때만 큐에 넣어둔다.
                    if (realUrl.IndexOf("board.php") >= 0)
                    {
                        int queryIndex = realUrl.IndexOf('?');
                        var queryString = HttpUtility.ParseQueryString(realUrl.Substring(queryIndex + 1));


                        var foundArticle = new ArticleInfo()
                        {
                            Subject = result.Title,
                            ID = long.Parse(queryString["wr_id"]),
                            Table = queryString["bo_table"]
                        };

                        // 담겨진 요소를 캐시해서 배열에서 요소를 다시 찾아보는낭비를 줄여야하지만
                        // 이 프로그램은 이정도 성능 이슈는 상관없으므로 무시하자.
                        if (myArticles.Where(x => (x.ID == foundArticle.ID && x.Table.Equals(foundArticle.Table))).Count() == 0)
                        {
                            searchArticles.Add(foundArticle);
                            //Console.WriteLine("[구굴링 결과] {0}" + foundArticle.Subject); 
                        }
                    }
                }

                if (search.HasNext == false)
                {
                    break;
                }

                await Task.Delay(1000);
            }
            return searchArticles;
        }

        public bool Describe()
        {
            Console.WriteLine("총 {0}개의 게시물에서 {1}개의 댓글을 찾았습니다.", Articles.Count, Comments.Count);
            Console.WriteLine("한번 삭제한글은 복구가 불가능합니다. 삭제하시려면 Y를 누르세요.");
            Console.Write("정말 삭제하시겠습니까? ");

            var keyInfo = Console.ReadKey();
            Console.Write("\n");

            if (keyInfo.KeyChar.ToString().ToUpper().Equals("Y"))
            {
                return true;
            }

            return false;
        }

        protected string GetRandomMessage()
        {
            int r = Rand.Next(RandomMessages.Count);

            return RandomMessages[r];
        }

        public async Task Destroy()
        {
            int cnt = 0;

            foreach (CommentInfo comment in Comments)
            {
                Console.Write("[{0}/{1}] {2} 게시판의 {3}번 글의 코멘트 {4}번 삭제", ++cnt, Comments.Count, comment.Table, comment.ArticleID, comment.CommentID);
                
                if (await Clien.UpdateComment(comment, GetRandomMessage()) == true)
                {
                    if (await Clien.DeleteComment(comment) == true)
                    {
                        Console.Write("완료\n");
                    }
                    else
                    {
                        Console.Write("수정 성공 - 삭제실패\n");
                    }
                }
                else
                {
                    Console.Write("실패\n");
                }

                await Task.Delay(1000);
            }

            cnt = 0;
            foreach (ArticleInfo article in Articles)
            {
                Console.Write("[{0}/{1}] {2} 게시판의 {3}번 글 삭제", ++cnt, Articles.Count, article.Table, article.ID);

                
                if (await Clien.UpdateArticle(article, GetRandomMessage(), GetRandomMessage()) == true)
                {
                    if (await Clien.DeleteArticle(article) == true)
                    {
                        Console.Write("완료\n");
                    }
                    else
                    {
                        Console.Write("수정 성공 - 삭제실패\n");
                    }
                }
                else
                {
                    Console.Write("실패\n");
                }

                await Task.Delay(1000);
            }
        }
    }
}
