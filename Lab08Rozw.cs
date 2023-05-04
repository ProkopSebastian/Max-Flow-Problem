using ASD.Graphs;
using System;
using System.Globalization;

namespace Lab08
{
    public class ColumbExpedition : MarshalByRefObject
    {
        private int hours_in_day = 12;
        public ColumbExpedition() { }
        /*

         * :param n: Liczba punktów z których można dokonać obserwacji
         * :param days: Maksymalna liczba dni na dotarcie do punktu
         * :param paths: Tablica krotek reprezentujących dwukierunkowe drogi pomiędzy punktami
         * :param ships: Tablica krotek reprezetująca pozycje i liczba załóg na poszczególnych statkach
         * :return: Maksymalna liczba pomiarów które uda się wykonać załodze Krzysztofa Kolumba
         **/
        public int Stage1(int n, int days, (int from, int to, int distance)[] paths, (int where, int num_of_crews)[] ships)
        {
            int warstwy = days * hours_in_day; // tyle godzin ile warstw
            int w = n * warstwy + 2; // liczba wierzchołków warstwy razy liczba wierzchołków w warstwie dodać wierzchołek na start i na koniec
            int koniec = w - 1; // sztuczne ujscie 
            int start = w - 2;

            DiGraph<int> graf = new DiGraph<int>(w); // skierowany żeby nie cofać sie w czasie

            // dodaj krawedz miedzy 2 i 5 ale miedzy 2 w wartwie aktualnej a 5 w warstwie aktualna + liczba godzin miedzy 2 a 5 (pod warunkiem ze jest w zakresie)
            for (int i = 0; i < warstwy; i++)
            {
                foreach (var e in paths)
                {
                    if (e.distance + i < warstwy) // czyli czy liczba warstw sie zgadza
                    {
                        graf.AddEdge(e.from + i * n, e.to + ((e.distance + i) * n), int.MaxValue);
                        graf.AddEdge(e.to + i * n, e.from + ((e.distance + i) * n), int.MaxValue);
                    }
                }

                // w każdej warstwie podłączam wierzchołek do niego samego w warstwie wyżej
                if (i + 1 < warstwy) 
                {
                    for (int j = 0; j < n; j++) // kazdy wierzcholek
                    {
                        graf.AddEdge(j + n * i, j + n * (i + 1), int.MaxValue);
                    }
                }
            }

            // każdy wierzchołek podłączamy do ujścia krawędzią o pojemności 1 -- to symbolizuje że siedzą tam sobie do końca
            for (int j = 0; j < n; j++)
            {
                graf.AddEdge(j + (warstwy - 1) * n, koniec, 1);
            }

            // Połącz sztuczny wierzchołek startowy z wierzchołkami oznaczającymi statki, krawędziami o pojemności takiej jak załoga -- robimy to tylko w początkowej warstwie
            foreach (var v in ships)
            {
                graf.AddEdge(start, v.where, v.num_of_crews);
            }

            // Znajdź maksymalny przepływ
            var (flowValue, f) = Flows.FordFulkerson(graf, start, koniec);

            return flowValue;
        }


        public int Stage2(int n, int days, (int from, int to, int distance)[] paths, (int where, int num_of_crews)[] ships)
        {
            int liczba_statków = ships.Length;
            int warstwy = days * (hours_in_day+1); // tyle godzin ile warstw i warstwy separatorów
            int w = n * warstwy + 2 + liczba_statków; // liczba wierzchołków warstwy razy liczba wierzchołków w warstwie dodać wierzchołek na start i na koniec
            int koniec = w - 1; // sztuczne ujscie 
            int start = w - 2;
            int statek = n * warstwy;

            DiGraph<int> graf = new DiGraph<int>(w); // skierowany żeby nie cofać sie w czasie

            // Powtarzam to samo dla każdej warstwy
            for (int i = 0; i < warstwy; i++)
            {
                foreach (var e in paths)
                {
                    if (e.distance + i < warstwy && (i / 13 == (i + e.distance) / 13)) // czy nie wychodzimy w warstwy innego dnia
                    {
                        graf.AddEdge(e.from + i * n, e.to + ((e.distance + i) * n), int.MaxValue);
                        graf.AddEdge(e.to + i * n, e.from + ((e.distance + i) * n), int.MaxValue);
                    }
                }

                // w każdej warstwie podłączam wierzchołek do niego samego w warstwie wyżej, chyba że warstwy należą do innych dni
                if (i + 1 < warstwy)
                {
                    if(i / 13 == (i + 1) / 13) // jeśli są w obrębie tego samego dnia
                    {
                        for (int j = 0; j < n; j++) // kazdy wierzcholek
                        {
                            graf.AddEdge(j + n * i, j + n * (i + 1), int.MaxValue);
                        }
                    }
                    else // jeśli nie są w obrębie tego samego dnia, czyli łączę z separatorem
                    {
                        for (int j = 0; j < n; j++) // kazdy wierzcholek
                        {
                            graf.AddEdge(j + n * i, j + n * (i + 1), 1);
                        }
                    }
                }
            }

            // każdy wierzchołek podłączamy do ujścia krawędzią o pojemności 1 -- to symbolizuje że siedzą tam sobie do końca
            for (int j = 0; j < n; j++)
            {
                graf.AddEdge(j + (warstwy - 1) * n, koniec, 1); // może zmienić na -2 przez nieużywany separator?
            }

            // Start łączę do statków oddzielnych od reszty
            int pom = 0;
            for (int i = statek; i < statek + liczba_statków; i++)
            {
                graf.AddEdge(start, i, ships[pom++].num_of_crews);
            }

            // Do każdej warstwy oprócz separatorów połącz statek z odpowiadającym mu wierzchołkiem
            for(int warstwa = 0; warstwa < warstwy; warstwa++)
            {
                if (warstwa % 13 == 0) // do warstw zaczynających nowe dnie nie chcemy dodawać, bo schodzi się godzinę
                    continue; 
                pom = 0;
                for(int st = statek; st < statek + liczba_statków; st++)
                {
                    graf.AddEdge(st, warstwa * n + ships[pom++].where, int.MaxValue);
                }
            }

            // Znajdź maksymalny przepływ
            var (flowValue, _) = Flows.FordFulkerson(graf, start, koniec); // discard dla nieużywanego f

            return flowValue;
        }
    }
}
