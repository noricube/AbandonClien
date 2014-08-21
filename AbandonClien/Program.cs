using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbandonClien
{
    class Program
    {
        static void Main(string[] args)
        {
            Clien clien;

            while(true)
            {
                string id;
                string pw;

                Console.Write("클리앙 아이디: ");
                id = Console.ReadLine();

                Console.Write("클리앙 암호: ");
                pw = Console.ReadLine();

                clien = new Clien(id,pw);
                var loginTask = clien.Login();
                loginTask.Wait();

                if ( loginTask.Result == false )
                {
                    Console.WriteLine("로그인 실패... 아이디 암호를 다시 확인하세요.");
                }

                Console.WriteLine("로그인 성공... 글을 수집합니다.");
                break;
            }

            var de = new DestroyEverything(clien);
            de.Collect().Wait();

            if ( de.Describe() == true )
            {
                de.Destroy().Wait();
            }

            Console.WriteLine("<엔터>키를 누르면 종료합니다.");
            Console.ReadLine();
        }
    }
}
