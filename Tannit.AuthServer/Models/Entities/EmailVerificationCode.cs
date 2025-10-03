public class EmailVerification
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Code { get; set; }
    public DateTime Expiration { get; set; }
    public bool IsUsed { get; set; } //sert à marquer si le code a déjà été utilisé ou non car il ne peut etre utilisé qu'une seule fois
}