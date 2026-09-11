namespace CoSpace.Domain;

public static class CatalogSeed
{
    public static IReadOnlyList<Sede> Sedes { get; } =
    [
        new("sede-centro-mayor", "Centro Mayor", "Calle 38A Sur # 34D-51, Antonio Nariño, Bogotá.", "Lunes a sábado, 7:00 a.m. - 9:00 p.m.", ["Wi-Fi", "Café", "Lockers"], "centro-mayor.jpg"),
        new("sede-santa-fe", "Centro Comercial Santa Fe", "Autopista Norte con Calle 183, Bogotá.", "Lunes a sábado, 7:00 a.m. - 9:00 p.m.", ["Wi-Fi", "Café", "Lockers"], "santa-fe.jpg"),
        new("sede-plaza-central", "Plaza Central", "Carrera 65 # 11-50, Puente Aranda, Bogotá.", "Lunes a sábado, 7:00 a.m. - 9:00 p.m.", ["Wi-Fi", "Café", "Lockers"], "plaza-central.jpg"),
        new("sede-mallplaza-nqs", "Mallplaza NQS", "Carrera 30 # 19, Los Mártires, Bogotá.", "Lunes a sábado, 7:00 a.m. - 9:00 p.m.", ["Wi-Fi", "Café", "Lockers"], "mallplaza-nqs.jpg"),
        new("sede-nuestro-bogota", "Nuestro Bogotá", "Av. Carrera 86 # 55A-75, Engativá, Bogotá.", "Lunes a sábado, 7:00 a.m. - 9:00 p.m.", ["Wi-Fi", "Café", "Lockers"], "nuestro-bogota.jpg")
    ];

    public static IReadOnlyList<Espacio> Espacios { get; } = Sedes.SelectMany(CreateSpaces).ToList();

    public static IReadOnlyList<Plan> Planes { get; } =
    [
        new("plan-basico", "Básico", 320000, ["Puesto flexible ilimitado en cualquier sede", "5h de salas de reunión"]),
        new("plan-pro", "Pro", 580000, ["Puesto flexible ilimitado", "Oficina privada por horas con descuento", "20h de salas de reunión"]),
        new("plan-empresarial", "Empresarial", 1450000, ["Oficina privada dedicada", "Salas ilimitadas", "Soporte prioritario"])
    ];

    private static IEnumerable<Espacio> CreateSpaces(Sede sede)
    {
        for (var index = 1; index <= 30; index++)
            yield return Space($"puesto-{sede.Id}-{index}", $"Puesto {index}", TipoEspacio.EscritorioFlexible, 1, sede, "Piso 2, Área abierta", 12000, 45000);
        for (var index = 1; index <= 15; index++)
            yield return Space($"oficina-{sede.Id}-{index}", $"Oficina {index}", TipoEspacio.OficinaPrivada, 2, sede, "Piso 2", 35000, 150000);
        for (var index = 1; index <= 10; index++)
            yield return Space($"cubiculo-{sede.Id}-{index}", $"Cubículo {index}", TipoEspacio.OficinaPrivada, 4, sede, "Piso 2", 55000, 220000);
        for (var index = 1; index <= 3; index++)
            yield return Space($"sala-alpha-{sede.Id}-{index}", $"Sala Alpha {index}", TipoEspacio.SalaReunion, 6, sede, "Piso 3", 70000, null);
        for (var index = 1; index <= 2; index++)
            yield return Space($"sala-beta-{sede.Id}-{index}", $"Sala Beta {index}", TipoEspacio.SalaReunion, 8, sede, "Piso 3", 95000, null);
        yield return Space($"sala-gamma-{sede.Id}", "Sala Gamma", TipoEspacio.SalaReunion, 12, sede, "Piso 3", 130000, null);
    }

    private static Espacio Space(string id, string name, TipoEspacio type, int capacity, Sede sede, string location, decimal hourly, decimal? daily) =>
        new(id, name, type, capacity, sede.Id, $"{sede.Nombre} · {location}", hourly, daily, EstadoEspacio.Activo, $"{id}.jpg");
}