using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace CrossingPoints
{
    public class TLabel
    {
        public int CallPoint; // Точка, откуда вызвали
        public int JumpPoint; // Точка, куда перешли
        
        public string Text;

        public TLabel(string Text, int CallPoint)
        {
            this.Text = Text;
            this.CallPoint = CallPoint;
        }
    }

    public class TOperator
    {
        public string Text;
        public int Coordinate;

        public TOperator(string Text, int Number)
        {
            this.Text = Text;
            this.Coordinate = Number;
        }
    }
    public class TBeginEnd
    {
        public string Text;
        public List<TLabel> LabelList;
        public List<TOperator> OperatorList; // Список операторов

        public TBeginEnd(string Text)
        {
            this.Text = Text;
        }
    }

    class metric_crossing_points
    {
        // Измените это поле перед запуском!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        static string path_to_file = @"C:\Documents and Settings\Admin\Рабочий стол\Metrix\C#_METRIC_crossing_points\Ref_To_glob_vars\Input_File_Code\BadCode_Delphi\GoTo_Break_Continue_Exit.~dpr";

        static void Main(string[] args)
        {
            string code_string;

            code_string = readCodeFromFile();  // Считываем исходный код с файла
            // Удаляем то, что может помешать правильно проанализировать код
            code_string = DeleteElement(code_string, @"\/\/[^\r\n]*");      // Удаляем однострочные комментарии //
            code_string = DeleteElement(code_string, @"{[\s\S]*?}");  // Удаляем многострочные комментарии { }
            code_string = DeleteElement(code_string, @"'[^\]][\s\S]*?'"); // Удаляем литералы ' '

            List<string> BeginEnd_StringList = getBeginEndList(code_string); // Выделяем в программе все подпрограммы

            List<TBeginEnd> BeginEnd_List = new List<TBeginEnd>();
            
            foreach (string item in BeginEnd_StringList)
                BeginEnd_List.Add(new TBeginEnd(item));

            int i;

            // Выделяем операторы 
            foreach (TBeginEnd BeginEndItem in BeginEnd_List)
            {
                int i_start = 0;
                BeginEndItem.OperatorList = new List<TOperator>();
                i = 0;
                int OperatorNumb = 1;
                while(i < BeginEndItem.Text.Length)
                {
                    if (BeginEndItem.Text[i] == ';')
                    {
                        string Operator = BeginEndItem.Text.Substring(i_start, i - i_start);

                        BeginEndItem.OperatorList.Add(new TOperator(Operator, OperatorNumb));

                        OperatorNumb++;
                        i++;
                        i_start = i;

                        while (i < BeginEndItem.Text.Length && isSpaceSymbol(BeginEndItem.Text[i]))
                        {
                            i++;
                            i_start = i;
                        }
                    }

                    i++;
                }
            }

            string gotoString = "goto";
            int indexFoundGoto;

            // Поиск goto (CallPoints) среди операторов
            foreach (TBeginEnd BeginEndItem in BeginEnd_List)
            {
                BeginEndItem.LabelList = new List<TLabel>();
                foreach (TOperator Operator in BeginEndItem.OperatorList)
                {
                    indexFoundGoto = Operator.Text.IndexOf(gotoString, 0, Operator.Text.Length, StringComparison.OrdinalIgnoreCase);
                    if (indexFoundGoto > -1)
                    {
                        i = indexFoundGoto + gotoString.Length;

                        while (isSpaceSymbol(Operator.Text[i]))
                            i++;

                        int i_startIdentifier = i;

                        while (i < Operator.Text.Length && isNotEndOfIdentifier(Operator.Text[i]))
                            i++;

                        string Identifier = Operator.Text.Substring(i_startIdentifier, i - i_startIdentifier);

                        BeginEndItem.LabelList.Add(new TLabel(Identifier, Operator.Coordinate));
                    }
                }
            }

            // Удаляем те подпрограммы, в которых нет labels (нет goto)
            i = 0;
            while (i < BeginEnd_List.Count)
            {
                if (BeginEnd_List[i].LabelList.Count == 0)
                    BeginEnd_List.Remove(BeginEnd_List[i]);
                else
                    i++;
            }

            // Поиск идентификаторов label (JumpPoints)
            int indexFoundLabel;
            foreach (TBeginEnd BeginEndItem in BeginEnd_List)
            {
                foreach (TOperator Operator in BeginEndItem.OperatorList)
                {
                    foreach (TLabel label in BeginEndItem.LabelList)
                    {
                        indexFoundLabel = Operator.Text.IndexOf(label.Text, 0, Operator.Text.Length, StringComparison.OrdinalIgnoreCase);
                        if (indexFoundLabel == 0)
                        {
                            label.JumpPoint = Operator.Coordinate;
                        }
                    }  

                }
            }

            // Удаляем labels, у которых точка вызова больше, чем точка прыжка.
            i = 0;
            while (i < BeginEnd_List.Count)
            {
                int j = 0;
                while (j < BeginEnd_List[i].LabelList.Count)
                    if (BeginEnd_List[i].LabelList[j].CallPoint > BeginEnd_List[i].LabelList[j].JumpPoint)
                        BeginEnd_List[i].LabelList.Remove(BeginEnd_List[i].LabelList[j]);
                    else
                        j++;

                i++;
            }

            foreach (TBeginEnd BeginEndItem in BeginEnd_List)
            {
                foreach (TLabel label in BeginEndItem.LabelList)
                {
                    if (label.CallPoint > label.JumpPoint)
                    {
                        BeginEndItem.LabelList.Remove(label);
                    }
                }
            }


            foreach (TBeginEnd BeginEndItem in BeginEnd_List)
            {
                foreach (TLabel label in BeginEndItem.LabelList)
                {
                    Console.WriteLine("a {0}, b {1}, : {2}", label.CallPoint, label.JumpPoint, label.Text);
                }
            }



            foreach (TBeginEnd BeginEndItem in BeginEnd_List)
            {
                foreach (TOperator Operator in BeginEndItem.OperatorList)
                {
                    Console.WriteLine("{0}: {1}", Operator.Coordinate, Operator.Text);
                }
            }

            int crossingPointsCount = 0;
            foreach (TBeginEnd BeginEndItem in BeginEnd_List)
            {
                for (i = 0; i < BeginEndItem.LabelList.Count; i++)
                    for (int j = 0; j < BeginEndItem.LabelList.Count; j++)
                    {
                        if (i != j)
                            if (isCrossingPoint(BeginEndItem.LabelList[i], BeginEndItem.LabelList[j]))
                                crossingPointsCount++;   
                    }
            }
            Console.WriteLine("======================================================");
            Console.WriteLine("CrossingPoints Count = {0}", crossingPointsCount);

            Console.Read();
        }

        public static string readCodeFromFile()
        {
            string returned_string;

            StreamReader stream_reader = new StreamReader(path_to_file);
            returned_string = stream_reader.ReadToEnd();
            return returned_string;

        }

        public static string DeleteElement(string source_string, string pattern)
        {
            string returned_string;
            
            Regex regular_expression = new Regex(pattern);
            returned_string = regular_expression.Replace(source_string, String.Empty);

            return returned_string;            
        }

        public static List<string> getBeginEndList(string source_string)
        {
            int indexFoundBegin = 0;
            int indexFoundEnd = 0;
            int i = 0;
            int i_start = 0;
            int depth;
            string strBegin = "begin";
            string strEnd = "end";
            string strCase = "case";
            List<string> BeginEndList = new List<string>();
            int indexFoundCase;

            // удалить end от case
            while (i < source_string.Length)
            {
                indexFoundCase = source_string.IndexOf(strCase, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);
                i = indexFoundCase + strCase.Length - 1;

                if (indexFoundCase > -1)
                {
                    indexFoundBegin = source_string.IndexOf(strBegin, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);
                    indexFoundEnd = source_string.IndexOf(strEnd, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);

                    depth = 1;
                    while (i < source_string.Length && depth != 0)
                    {
                        if (indexFoundBegin > -1 && indexFoundBegin < indexFoundEnd)
                        {
                            depth++;
                            i = indexFoundBegin + strBegin.Length - 1;
                            indexFoundBegin = source_string.IndexOf(strBegin, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);
                        }
                        else if (indexFoundEnd > -1)
                        {
                            depth--;
                            if (depth == 0)
                            {
                                source_string = source_string.Remove(indexFoundEnd, strEnd.Length + 1); // Удаляем последний end
                                i = indexFoundEnd;
                                break;
                            }
                            i = indexFoundEnd + strEnd.Length - 1;
                            indexFoundEnd = source_string.IndexOf(strEnd, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
                else
                    break;
            }

            while (i_start < source_string.Length && i_start > -1)
            {
                indexFoundBegin = source_string.IndexOf(strBegin, i_start, source_string.Length - i_start, StringComparison.OrdinalIgnoreCase);
                i_start = indexFoundBegin;
                if (indexFoundBegin > -1)
                {
                    i = indexFoundBegin + strBegin.Length - 1;
                    depth = 1;
                    
                    indexFoundBegin = source_string.IndexOf(strBegin, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);
                    indexFoundEnd = source_string.IndexOf(strEnd, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);

                    while (i < source_string.Length && depth != 0)
                    {
                        if (indexFoundBegin > -1 && indexFoundBegin < indexFoundEnd)
                        {
                            depth++;
                            i = indexFoundBegin + strBegin.Length - 1;
                            indexFoundBegin = source_string.IndexOf(strBegin, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);
                        }
                        else if (indexFoundEnd > -1)
                        {
                            depth--;
                            i = indexFoundEnd + strEnd.Length - 1;
                            indexFoundEnd = source_string.IndexOf(strEnd, i, source_string.Length - i, StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    BeginEndList.Add(source_string.Substring(i_start, i - i_start + 2));
                    i_start = i;
                }
            }

            return BeginEndList;
        }

        public static bool isNotEndOfIdentifier(char Chr)
        {
            bool Result = false;

            if (( (Chr >= 'A' && Chr <= 'Z') || (Chr >= 'a' && Chr <= 'z') || (Chr >= '0' && Chr <= '9') || (Chr == '_') ))    
                Result = true;

            return Result;
        }

        public static bool isSpaceSymbol(char Chr)
        {
            bool Result = false;

            if (Chr == ' ' || Chr == '\t' || Chr == '\n' || Chr == '\r')
                Result = true;

            return Result;
        }

        public static bool isNeedToAdd(List<TLabel> LabelList, string Identifier)
        {
            bool Result = true;
            foreach (TLabel label in LabelList)
            {
                if (label.Text == Identifier)
                {
                    Result = false;
                    break;
                }
            }

            return Result;
        }

        public static bool isCrossingPoint(TLabel iLabel, TLabel jLabel)
        {
            bool Result = false;
            if (jLabel.CallPoint < iLabel.CallPoint && iLabel.CallPoint < jLabel.JumpPoint &&
                jLabel.JumpPoint < iLabel.JumpPoint)
                Result = true;

            return Result;
        }
    }
}

