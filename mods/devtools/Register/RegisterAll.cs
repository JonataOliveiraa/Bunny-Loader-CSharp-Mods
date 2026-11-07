namespace DevToolMod.Register;

public static class RegisterAll
{
    public static void Run()
    {
        RegisterInterfaces.RegisterInterfaceLayers();
        RegisterInterfaces.RegisterAllInterfaces();
    }
}
