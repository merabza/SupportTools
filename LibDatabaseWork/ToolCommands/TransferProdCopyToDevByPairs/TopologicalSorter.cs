using System.Collections.Generic;
using System.Linq;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//Kahn-ის ალგორითმი: შემოსავალი — ცხრილების სია და FkEdge-ების სია. შედეგი — ან ორდერი (referenced ცხრილი ჯერ),
//ან ციკლის შემცველი ცხრილების სია.
internal static class TopologicalSorter
{
    public static SortResult Sort(IReadOnlyList<(string Schema, string Table)> nodes, IReadOnlyList<FkEdge> edges)
    {
        var nodeSet = new HashSet<(string Schema, string Table)>(nodes);

        //ვითვალისწინებთ მხოლოდ იმ edge-ებს, რომელთა ორივე ბოლო პოვნადია nodes-ში; self-reference იგნორდება
        List<FkEdge> relevantEdges = edges.Where(e =>
            nodeSet.Contains((e.FromSchema, e.FromTable)) && nodeSet.Contains((e.ToSchema, e.ToTable)) &&
            !(e.FromSchema == e.ToSchema && e.FromTable == e.ToTable)).ToList();

        //adjacency: To → set of From (referenced table → list of tables that depend on it)
        var dependents = new Dictionary<(string Schema, string Table), HashSet<(string Schema, string Table)>>();
        var incomingCount = new Dictionary<(string Schema, string Table), int>();
        foreach ((string Schema, string Table) n in nodes)
        {
            dependents[n] = [];
            incomingCount[n] = 0;
        }

        foreach (FkEdge e in relevantEdges)
        {
            (string Schema, string Table) from = (e.FromSchema, e.FromTable);
            (string Schema, string Table) to = (e.ToSchema, e.ToTable);
            //from-ი ჯერ უნდა იყოს ჩასმული to-ის შემდეგ → ანუ to → from edge
            if (dependents[to].Add(from))
            {
                incomingCount[from] += 1;
            }
        }

        var queue = new Queue<(string Schema, string Table)>();
        foreach (KeyValuePair<(string Schema, string Table), int> kvp in incomingCount)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        var ordered = new List<(string Schema, string Table)>();
        while (queue.Count > 0)
        {
            (string Schema, string Table) node = queue.Dequeue();
            ordered.Add(node);
            foreach ((string Schema, string Table) dep in dependents[node])
            {
                incomingCount[dep] -= 1;
                if (incomingCount[dep] == 0)
                {
                    queue.Enqueue(dep);
                }
            }
        }

        if (ordered.Count == nodes.Count)
        {
            return new SortResult(ordered, null);
        }

        //ციკლი არსებობს: ციკლის შემცველ ცხრილებად ვცილცილდებით ყველაფერს, რაც ordered-ში არ მოყვა
        List<(string Schema, string Table)> cycle = nodes.Where(n => incomingCount[n] > 0).ToList();
        return new SortResult(null, cycle);
    }

    public sealed class SortResult
    {
        public SortResult(List<(string Schema, string Table)>? ordered, List<(string Schema, string Table)>? cycle)
        {
            Ordered = ordered;
            Cycle = cycle;
        }

        public List<(string Schema, string Table)>? Ordered { get; }
        public List<(string Schema, string Table)>? Cycle { get; }
        public bool HasCycle => Cycle is not null;
    }
}
