using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            RandomMessages.Add("내용");
            RandomMessages.Add("펑");
        }


        public async Task Collect()
        {
            List<ArticleInfo> articles = await Clien.GetMyArticles();

            Console.WriteLine("총 {0}개의 게시물에 댓글을 검색합니다.", articles.Count);

            List<Task> tasks = new List<Task>();
            var commentBag = new ConcurrentBag<CommentInfo>();

            int cnt = 0;
            // 내가 쓴글에서 코멘트를 검색한다.
            foreach (var article in articles)
            {
                Console.WriteLine("[{0}/{1}] {2}", ++cnt, articles.Count, article.Subject);

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

        public bool Describe()
        {
            Console.WriteLine("총 {0}개의 게시물에서 {1}개의 댓글을 찾았습니다.", Articles.Count, Comments.Count);
            Console.WriteLine("한번 삭제한글은 복구가 불가능합니다. 삭제하시려면 Y를 누르세요.");
            Console.Write("정말 삭제하시겠습니까? ");

            var keyInfo = Console.ReadKey();
            Console.Write("\n");

            if ( keyInfo.KeyChar.ToString().ToUpper().Equals("Y"))
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
            foreach(CommentInfo comment in Comments)
            {
                Console.WriteLine("[{0}/{1}] {2} 게시판의 {3}번 글의 코멘트 {4}번 삭제", ++cnt, Comments.Count, comment.Table, comment.ArticleID, comment.CommentID);
                //await Clien.UpdateComment(comment, GetRandomMessage());
                //await Clien.DeleteComment(comment);
                
            }
        }
    }
}
