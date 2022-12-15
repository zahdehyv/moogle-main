namespace MoogleEngine;

using System.Text;

public static class Moogle
{
    public static DatabaseItem Data = DatabaseRefresh();
    public static DatabaseItem StartRefresh()
    {
        return Data;
    }

    private static bool scoreiszero(SearchItem scoar)
    {
        return scoar.Score == 0;
    }

    static bool textisNullorSpace(string intra)
    {
        return (intra == "" || intra == " ");
    }

    static bool LenghIsBig(string intra)
    {
        return intra.Length > 45;
    }

    public static SearchResult Query(string query, DatabaseItem Data)
    {
        //start of the search

        var listed_query = Normalize(query, true, false).Split().ToList().Distinct().ToList();
        listed_query.RemoveAll(textisNullorSpace);
        var operatmatrix = Operators(Normalize(query, true, true).Split().ToList(), listed_query);

        var presearchitems = new List<SearchItem>();

        for (int i = 0; i < Data.Files.Count; i++)
        {
            double scr = 0;
            foreach (var elem in listed_query)
            {
                if (operatmatrix[0][listed_query.IndexOf(elem), 0] > 0)
                {
                    if ((Data.FilesWords[i].Contains(elem)))
                    {
                        scr = 0;
                        break;
                    }
                }
                if (operatmatrix[0][listed_query.IndexOf(elem), 1] > 0)
                {
                    if (!(Data.FilesWords[i].Contains(elem)))
                    {
                        scr = 0;
                        break;
                    }
                }
                if (Data.FilesWords[i].Contains(elem))
                {
                    scr = scr + ((Data.TfIdf[i, Data.Words[elem]]) * Math.Pow(operatmatrix[0][listed_query.IndexOf(elem), 2], operatmatrix[0][listed_query.IndexOf(elem), 2]));
                }
            }

            //AQUI SE IMPLEMENTA EL OPERADOR DE CERCANIA
            for (int j = 1; j < operatmatrix.Count; j++)
            {
                if ((Data.FilesWords[i].Contains(listed_query[operatmatrix[j][0, 0]])) && (Data.FilesWords[i].Contains(listed_query[operatmatrix[j][1, 0]])))
                {
                    int a = Data.FilesWords[i].Count;
                    List<int> b = new List<int>();
                    List<int> c = new List<int>();
                    for (int k = 0; k < a; k++)
                    {
                        if (Data.FilesWords[i][k] == listed_query[operatmatrix[j][0, 0]])
                        {
                            b.Add(k);
                        }
                        else if (Data.FilesWords[i][k] == listed_query[operatmatrix[j][1, 0]])
                        {
                            c.Add(k);
                        }
                    }
                    List<int> d = new List<int>();
                    foreach (var itb in b)
                    {
                        foreach (var itc in c)
                        {
                            if (itb > itc)
                            {
                                d.Add(itb - itc);
                            }
                            else
                            {
                                d.Add(itc - itb);
                            }

                        }
                    }
                    double e = d.Min();
                    Console.WriteLine();
                    Console.Write($"Distancia minima entre {listed_query[operatmatrix[j][0, 0]]} y {listed_query[operatmatrix[j][1, 0]]} en {Data.Files[i]}: ");
                    Console.Write(e);
                    Console.WriteLine();
                    Console.Write($"Anyadido {scr * Math.Log(a / e)}");
                    scr = scr + (scr * Math.Log(a / e));
                    Console.WriteLine();


                }
            }
            scr=Compress(scr);
            presearchitems.Add(new SearchItem(Path.GetFileNameWithoutExtension(Data.Files[i]), GenerarSnippet(scr, listed_query, i, 60, Data), scr, GenerarURL(Data.Files[i], Data)));
        }

        presearchitems.RemoveAll(scoreiszero);
        var ordereditems = presearchitems.OrderByDescending(x => x.Score).ToArray();
        Console.WriteLine();
        Console.WriteLine($"Busqueda de {query} con {ordereditems.Count(x => x.Score != 0)} archivos con score distinto de 0");
        foreach (var item in ordereditems)
        {
            Console.WriteLine($"archivo {item.Title} con score {item.Score}");
        }

        return new SearchResult(ordereditems, ConstruirSugerencia(listed_query, Data));
    }

    //Esto es para q abra el txt desde el servidor
    static string GenerarURL(string initurl, DatabaseItem Data)
    {
        StringBuilder midurl = new StringBuilder();
        midurl.Append(initurl);
        midurl.Remove(3, 21);
        return midurl.ToString();
    }

    //Esto crea el Snippet
    static List<string> GenerarSnippet(double valscr, List<string> listquery, int filepos, int longitud, DatabaseItem Data)
    {
        if (valscr == 0)
        {
            return new List<string>();
        }
        List<string> esnipet = new List<string>();
        int palabrasdeldocumento = Data.FilesWords[filepos].Count;

        if (palabrasdeldocumento < longitud || palabrasdeldocumento == longitud)
        {
            for (int i = 0; i < palabrasdeldocumento; i++)
            {
                esnipet.Add(Data.FilesWords[filepos][i]);

            }
            return esnipet;
        }
        else
        {
            double curr = 0;
            int indexofmostimportant = 0;
            foreach (var item in listquery)
            {
                if (Data.Words.Keys.ToList().Contains(item))
                {
                    if (Data.TfIdf[filepos, Data.Words[item]] > curr)
                    {
                        curr = Data.TfIdf[filepos, Data.Words[item]];
                        indexofmostimportant = listquery.IndexOf((string)item);
                    }
                }

            }
            var thewordindex = Data.FilesWords[filepos].IndexOf(listquery[indexofmostimportant]);

            int start = Math.Max(0, (thewordindex - (longitud - 1)));
            int end = Math.Min((palabrasdeldocumento - 1 - longitud), (thewordindex + 1));
            int maxindex = start;

            double currentcount = 0;
            for (int j = start; j < start + longitud; j++)
            {
                currentcount = currentcount + (Data.TfIdf[filepos, Data.Words[Data.FilesWords[filepos][j]]] + 1000 * Enumerable.Count<string>(listquery, (Func<string, bool>)(x => x == Data.FilesWords[filepos][j])));
            }
            double max = currentcount;
            for (int i = start + 1; i < end; i++)
            {
                currentcount = currentcount - (Data.TfIdf[filepos, Data.Words[Data.FilesWords[filepos][i - 1]]] + 1000 * Enumerable.Count<string>(listquery, (Func<string, bool>)(x => x == Data.FilesWords[filepos][i - 1])));
                currentcount = currentcount + (Data.TfIdf[filepos, Data.Words[Data.FilesWords[filepos][i + longitud]]] + 1000 * Enumerable.Count<string>(listquery, (Func<string, bool>)(x => x == Data.FilesWords[filepos][i + longitud])));

                if (currentcount > max)
                {
                    max = currentcount;
                    maxindex = i;
                }
            }

            for (int i = 0; i < longitud; i++)
            {
                esnipet.Add(Data.FilesWords[filepos][maxindex + i]);
            }
            return esnipet;
        }
    }


    public static DatabaseItem DatabaseRefresh()
    {
        var start = DateTime.Now.Ticks;
        //creando el indice archivos
        Console.WriteLine("creando indice de archivos...");
        var tempfiles = index("../MoogleServer/wwwroot/Content/");
        // Console.Clear();


        //creando lista de listas con las palabras de cada documento
        Console.WriteLine("creando lista de lista de palabras...");
        var tempfileswords = new List<List<string>>();
        foreach (var item in tempfiles)
        {
            tempfileswords.Add(Normalize(System.IO.File.ReadAllText(item), true, false).Split().ToList());
        }
        // Console.Clear();

        foreach (var item in tempfileswords)
        {
            item.RemoveAll(textisNullorSpace);
            item.RemoveAll(LenghIsBig);
        }



        //creando indice de palabras
        Console.WriteLine("creando indice de palabras...");
        var tempword = new List<string>();

        foreach (var item in tempfileswords)
        {
            tempword.AddRange(item);
        }
        var tempwords = tempword.Distinct().ToArray();

        //Creando dicc de palabras y posicion...

        Dictionary<string, int> tempwordsdicc = new Dictionary<string, int>();
        for (int i = 0; i < tempwords.Length; i++)
        {
            tempwordsdicc.Add(tempwords[i], i);
        }

        // foreach (var item in tempfileswords)
        // {
        //     foreach (var obj in item)
        //     {
        //         if (!(tempwords.Contains(obj)))
        //         {
        //             tempwords.Add(obj);
        //         }
        //     }
        // }
        // Console.Clear();


        var files_count = tempfiles.Count;
        double b = files_count;
        var words_count = tempwords.Length;
        // creando matriz con los tf por documento
        var matriz_TF = new double[files_count, words_count];

        for (int i = 0; i < files_count; i++)
        {
            // Console.Clear(); 
            Console.WriteLine($"scanning document {Path.GetFileName(tempfiles[i])}");

            for (int j = 0; j < tempfileswords[i].Count; j++)
            {

                matriz_TF[i, tempwordsdicc[tempfileswords[i][j]]]++;

            }
        }

        //creando matriz de tf-idf comprimido
        var matriz_TFIDF = new double[files_count, words_count];
        for (int j = 0; j < words_count; j++)
        {

            double curr_idf = 0;
            for (int i = 0; i < files_count; i++)
            {
                if (!(matriz_TF[i, j] == 0))
                {
                    curr_idf++;
                }
            }
            // if (tempwords[j]=="de")
            // {
            //     Console.WriteLine("de");
            //     Console.WriteLine($"IDF:{curr_idf}");
            // }

            for (int i = 0; i < files_count; i++)
            {
                if (matriz_TF[i, j] != 0 && curr_idf != files_count)
                {
                    matriz_TFIDF[i, j] = TF_IDF_Calc(matriz_TF[i, j], Convert.ToDouble(tempfileswords[i].Count), b, curr_idf);
                }
                //     if (tempwords[j]=="de")
                // {
                //     Console.WriteLine($"TFIDF[{i}]:{matriz_TFIDF[i, j]}");
                //     Console.WriteLine($"valores usados:");
                //     Console.WriteLine($"TF:{matriz_TF[i, j]}");
                //     Console.WriteLine($"Cantidad de palabras convertido:{Convert.ToDouble(tempfileswords[i].Count)}, sin convertir:{tempfileswords[i].Count}");
                // Console.WriteLine($"Cantidad de Archivos:{b}");
                // }

            }
        }
        // Console.Clear();
        var end = DateTime.Now.Ticks;
        var totalseconds = ((end - start) / 10000) / 1000;
        int minutes = Convert.ToInt32(totalseconds / 60);
        var seconds = (totalseconds % 60);
        Console.WriteLine($"fueron cargados {files_count} documentos con {words_count} palabras diferentes en {minutes}m {seconds}s");



        return new DatabaseItem(tempfiles, tempwordsdicc, tempwords, tempfileswords, matriz_TFIDF);
    }

    static double TF_IDF_Calc(double ConteoPalabraDocActual, double TotalPalabrasDocActual, double TotalArchivos, double TotalDocsconPalabra)
    {
        return Compress(((ConteoPalabraDocActual / TotalPalabrasDocActual) * Math.Log(TotalArchivos / TotalDocsconPalabra))*100);
    }

    static string ConstruirSugerencia(List<string> fromquery, DatabaseItem Data)
    {
        bool todosbien = true;
        foreach (var item in fromquery)
        {
            if (!Data.WordsList.Contains(item))
            {
                todosbien = false;
            }
        }

        if (todosbien) { return ""; }
        else
        {
            StringBuilder presuggest = new StringBuilder();
            foreach (var item in fromquery)
            {
                if (Data.WordsList.Contains(item))
                {
                    presuggest.Append(item);
                    presuggest.Append(" ");
                }
                else
                {
                    int eddis = 5;
                    int indexzero = 0;
                    int tfidfdifzero = -1;
                    for (int i = 0; i < Data.Words.Count; i++)
                    {
                        if (Edit_Distance(item, Data.WordsList[i]) < eddis)
                        {
                            eddis = Edit_Distance(item, Data.WordsList[i]);
                            indexzero = i;
                            int count = 0;
                            for (int j = 0; j < Data.Files.Count; j++)
                            {
                                if (Data.TfIdf[j, i] != 0)
                                {
                                    count++;
                                }
                            }
                            tfidfdifzero = count;
                        }
                        else if (Edit_Distance(item, Data.WordsList[i]) == eddis)
                        {
                            int count = 0;
                            for (int j = 0; j < Data.Files.Count; j++)
                            {
                                if (Data.TfIdf[j, i] != 0)
                                {
                                    count++;
                                }
                            }
                            if (count > tfidfdifzero)
                            {
                                tfidfdifzero = count;
                                indexzero = i;
                            }

                        }
                    }
                    if (eddis < 4)
                    {
                        presuggest.Append(Data.WordsList[indexzero]);
                        presuggest.Append(" ");
                    }
                    else
                    {
                        presuggest.Append(item);
                        presuggest.Append(" ");
                    }
                }
            }
            return presuggest.Remove(presuggest.Length - 1, 1).ToString();
        }
    }

    //Metodos utiles usados

    //Edit Distance para encontrar palabras "semejantes"

    static int Edit_Distance(string l, string ll)
    {
        int[,] cuadro = new int[l.Length + 1, ll.Length + 1];
        for (int i = 0; i < l.Length + 1; i++)
        {
            cuadro[i, 0] = i;
        }
        for (int i = 1; i < ll.Length + 1; i++)
        {
            cuadro[0, i] = i;
        }


        for (int i = 1; i < l.Length + 1; i++)
        {
            for (int j = 1; j < ll.Length + 1; j++)
            {
                if (l[i - 1] == ll[j - 1])
                {
                    cuadro[i, j] = (Math.Min(cuadro[i - 1, j - 1], Math.Min(cuadro[i - 1, j], (cuadro[i, j - 1]))));
                }
                else
                {
                    cuadro[i, j] = (Math.Min(cuadro[i - 1, j - 1], Math.Min(cuadro[i - 1, j], (cuadro[i, j - 1])))) + 1;
                }
            }
        }


        return cuadro[l.Length, ll.Length];
    }

    //Normalizer for the text
    public static string Normalize(string segment, bool letters, bool symbols)
    {
        int longitud = 0;
        int posxletter = 0;
        StringBuilder alpha = new StringBuilder();
        StringBuilder presymbnorm = new StringBuilder();
        StringBuilder presymbcerc = new StringBuilder();
        StringBuilder pre_a_alt = new StringBuilder();
        StringBuilder pre_e_alt = new StringBuilder();
        StringBuilder pre_i_alt = new StringBuilder();
        StringBuilder pre_o_alt = new StringBuilder();
        StringBuilder pre_u_alt = new StringBuilder();
        if (letters)
        {
            alpha.Append("qwertyuiopasdfghjklñzxcvbnm");
            pre_a_alt.Append("àáâäãåāą");
            pre_e_alt.Append("ęëēėèéê");
            pre_i_alt.Append("įīìîíï");
            pre_o_alt.Append("õōòöôó");
            pre_u_alt.Append("ûūüùú");
        }
        if (symbols)
        {
            presymbnorm.Append("*!^");
            presymbcerc.Append("~");
        }
        alpha.Append(" ");
        var alphabet = alpha.ToString();
        var symbnorm = presymbnorm.ToString();
        var symbcerc = presymbcerc.ToString();
        var a_alt = pre_a_alt.ToString();
        var e_alt = pre_e_alt.ToString();
        var i_alt = pre_i_alt.ToString();
        var o_alt = pre_o_alt.ToString();
        var u_alt = pre_u_alt.ToString();

        string lowercase_segment = segment.ToLower();
        foreach (char chr in lowercase_segment)
        {
            if (alphabet.Contains(chr) || a_alt.Contains(chr) || e_alt.Contains(chr) || i_alt.Contains(chr) || o_alt.Contains(chr) || u_alt.Contains(chr) || ".,".Contains(chr))
                longitud++;
            else if (symbnorm.Contains(chr))
            {
                longitud++;
            }
            else if (symbcerc.Contains(chr))
            {
                longitud++;
                longitud++;
                longitud++;
            }
            else if ("~".Contains(chr) && !symbols)
            {
                longitud++;
            }
        }

        char[] letter = new char[longitud];

        foreach (char chr in lowercase_segment)
        {
            if (alphabet.Contains(chr))
            {
                letter[posxletter] = chr;
                posxletter++;
            }
            else if (a_alt.Contains(chr))
            {
                letter[posxletter] = 'a';
                posxletter++;
            }
            else if (e_alt.Contains(chr))
            {
                letter[posxletter] = 'e';
                posxletter++;
            }
            else if (i_alt.Contains(chr))
            {
                letter[posxletter] = 'i';
                posxletter++;
            }
            else if (o_alt.Contains(chr))
            {
                letter[posxletter] = 'o';
                posxletter++;
            }
            else if (u_alt.Contains(chr))
            {
                letter[posxletter] = 'u';
                posxletter++;
            }
            else if (".,".Contains(chr))
            {
                letter[posxletter] = ' ';
                posxletter++;
            }
            else if (symbnorm.Contains(chr))
            {
                letter[posxletter] = chr;
                posxletter++;
            }
            else if (symbcerc.Contains(chr))
            {
                letter[posxletter] = ' ';
                posxletter++;
                letter[posxletter] = chr;
                posxletter++;
                letter[posxletter] = ' ';
                posxletter++;
            }
            else if ("~".Contains(chr) && !symbols)
            {
                letter[posxletter] = ' ';
                posxletter++;
            }
        }
        if (longitud != 0)
        {
            string word = string.Join("", letter);
            return word;
        }
        else
            return "";

    }

    //aqui voy a meter un metodo q comprime los tf en 4 con un ratio de 5 en 6 con un ratio de 10 en
    //8 con un ratio de 20 y en 9 con un ratio de 40.....y pone un limite en 10

    static double Compress(double a)
    {
        if (a > 1)
        {
            a = a - 1;
            a = a / 1.5;
            a = a + 1;
        }
        if (a > 2)
        {
            a = a - 2;
            a = a / 2;
            a = a + 2;
        }
        if (a > 4)
        {
            a = a - 4;
            a = a / 5;
            a = a + 4;
        }
        if (a > 8)
        {
            a = a - 8;
            a = a / 100;
            a = a + 8;
        }
        if (a > 10)
        {
            a = a - 10;
            a = a / 1000;
            a = a + 10;
        }
        return a;
    }

    //Esto devuelve la matriz de los operadores
    static int[] AddOperators(string texto, int[] extracted_op)
    {
        if (texto.Length == 1)
            return extracted_op;

        if (texto.StartsWith("!"))
        {
            extracted_op[0]++;
            return AddOperators(texto.Substring(1), extracted_op);
        }
        else if (texto.StartsWith("^"))
        {
            extracted_op[1]++;
            return AddOperators(texto.Substring(1), extracted_op);
        }
        else if (texto.StartsWith("*"))
        {
            extracted_op[2]++;
            return AddOperators(texto.Substring(1), extracted_op);
        }
        else
        {
            return extracted_op;
        }

    }

    static List<int[,]> Operators(List<string> querywithmods, List<string> querywithoutmods)
    {
        querywithmods.RemoveAll(textisNullorSpace);
        //modificadores en orden de aplicacion ! ^ * ~
        // sin embargo el primero que sacare es el ~ pq me altera el indice
        var listofoperators = new List<int[,]>();
        listofoperators.Add(new int[querywithoutmods.Count, 3]);
        for (int i = 0; i < querywithmods.Count; i++)
        {
            if ((i != querywithmods.Count - 1) && (querywithmods[i] == "~") && (querywithoutmods.Contains(Normalize(querywithmods[i + 1], true, false)) && querywithoutmods.Contains(Normalize(querywithmods[i - 1], true, false))))
            {
                listofoperators.Add(new int[2, 1]);
                listofoperators[listofoperators.Count - 1][0, 0] = querywithoutmods.IndexOf(Normalize(querywithmods[i - 1], true, false));
                listofoperators[listofoperators.Count - 1][1, 0] = querywithoutmods.IndexOf(Normalize(querywithmods[i + 1], true, false));
            }
        }
        int a;
        int[] b = new int[3];
        foreach (var item in querywithmods)
        {
            if (querywithoutmods.Contains(Normalize(item, true, false)))
            {
                a = querywithoutmods.IndexOf(Normalize(item, true, false));
                b = AddOperators(item, new int[3]);
                listofoperators[0][a, 0] += b[0];
                listofoperators[0][a, 1] += b[1];
                listofoperators[0][a, 2] += b[2] + 1;
            }
        }


        Console.WriteLine();
        Console.WriteLine();
        foreach (var item in querywithoutmods)
        {
            Console.Write(item);
            Console.Write(" ");
        }
        Console.WriteLine();
        foreach (var item in querywithmods)
        {
            Console.Write(item);
            Console.Write(" ");
        }
        Console.WriteLine();

        for (int i = 0; i < querywithoutmods.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Console.Write(listofoperators[0][i, j]);
                Console.Write(" ");
            }
            Console.WriteLine();
        }
        Console.WriteLine("indices vinculados");
        for (int i = 1; i < listofoperators.Count; i++)
        {
            Console.WriteLine($"{listofoperators[i][0, 0]} {listofoperators[i][1, 0]}");
        }

        Console.WriteLine();


        return listofoperators;
    }


    //Devuelve todos los archivos en el directorio
    public static List<string> index(string url)
    {
        List<string> file = new List<string>();

        List<string> urlFiles(string url)
        {

            file.AddRange(Directory.GetFiles(url).ToList());
            foreach (string u in Directory.GetDirectories(url))
            {
                urlFiles(u);
            }
            return file;
        }
        return urlFiles(url);
    }

}

