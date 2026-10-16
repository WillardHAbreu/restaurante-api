using Microsoft.EntityFrameworkCore;
using Restaurante.Api.Dados;
using Restaurante.Api.Dtos;
using Restaurante.Api.Modelos;
using Restaurante.Api.Resultado;

namespace Restaurante.Api.Servicos;

public class RestauranteServico
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public RestauranteServico(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    // --- AUTENTICAÇÃO ---
    public async Task<Resultado<TokenDto>> RegistrarAsync(RegistrarDto dto)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
        {
            return Resultado<TokenDto>.Erro("E-mail já cadastrado.", TipoErro.Conflito);
        }

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            Papel = "cliente" // O registro público sempre cria como "cliente"
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var token = _tokenService.GerarToken(usuario);
        return Resultado<TokenDto>.Ok(new TokenDto(token, usuario.Email, usuario.Papel));
    }

    public async Task<Resultado<TokenDto>> LoginAsync(LoginDto dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
        {
            return Resultado<TokenDto>.Erro("E-mail ou senha inválidos.", TipoErro.Validacao);
        }

        var token = _tokenService.GerarToken(usuario);
        return Resultado<TokenDto>.Ok(new TokenDto(token, usuario.Email, usuario.Papel));
    }

    // --- MESAS (Entidade Principal) ---
    public async Task<object> ListarMesasAsync(string? busca, int pagina, int tamanho)
    {
        var query = _context.Mesas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.ToLower();
            query = query.Where(m => m.NumeroOuNome.ToLower().Contains(termo) || m.Localizacao.ToLower().Contains(termo));
        }

        var total = await query.CountAsync();
        var itens = await query
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(m => new MesaRespostaDto(m.Id, m.NumeroOuNome, m.CapacidadePessoas, m.Localizacao))
            .ToListAsync();

        return new
        {
            Total = total,
            Pagina = pagina,
            Tamanho = tamanho,
            TotalPaginas = (int)Math.Ceiling(total / (double)tamanho),
            Itens = itens
        };
    }

    public async Task<Resultado<MesaRespostaDto>> ObterMesaPorIdAsync(int id)
    {
        var mesa = await _context.Mesas.FindAsync(id);
        if (mesa == null) return Resultado<MesaRespostaDto>.Erro("Mesa não encontrada.", TipoErro.NaoEncontrado);

        return Resultado<MesaRespostaDto>.Ok(new MesaRespostaDto(mesa.Id, mesa.NumeroOuNome, mesa.CapacidadePessoas, mesa.Localizacao));
    }

    public async Task<Resultado<MesaRespostaDto>> CriarMesaAsync(CriarMesaDto dto)
    {
        var mesa = new Mesa
        {
            NumeroOuNome = dto.NumeroOuNome,
            CapacidadePessoas = dto.CapacidadePessoas,
            Localizacao = dto.Localizacao
        };

        _context.Mesas.Add(mesa);
        await _context.SaveChangesAsync();

        return Resultado<MesaRespostaDto>.Ok(new MesaRespostaDto(mesa.Id, mesa.NumeroOuNome, mesa.CapacidadePessoas, mesa.Localizacao));
    }

    public async Task<Resultado<MesaRespostaDto>> AtualizarMesaAsync(int id, AtualizarMesaDto dto)
    {
        var mesa = await _context.Mesas.FindAsync(id);
        if (mesa == null) return Resultado<MesaRespostaDto>.Erro("Mesa não encontrada.", TipoErro.NaoEncontrado);

        mesa.NumeroOuNome = dto.NumeroOuNome;
        mesa.CapacidadePessoas = dto.CapacidadePessoas;
        mesa.Localizacao = dto.Localizacao;

        await _context.SaveChangesAsync();
        return Resultado<MesaRespostaDto>.Ok(new MesaRespostaDto(mesa.Id, mesa.NumeroOuNome, mesa.CapacidadePessoas, mesa.Localizacao));
    }

    public async Task<Resultado<bool>> DeletarMesaAsync(int id)
    {
        var mesa = await _context.Mesas.FindAsync(id);
        if (mesa == null) return Resultado<bool>.Erro("Mesa não encontrada.", TipoErro.NaoEncontrado);

        _context.Mesas.Remove(mesa);
        await _context.SaveChangesAsync();
        return Resultado<bool>.Ok(true);
    }

    // --- RESERVAS (Segunda Entidade + Regra do 409) ---
    public async Task<Resultado<ReservaRespostaDto>> CriarReservaAsync(CriarReservaDto dto, int usuarioId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var mesa = await _context.Mesas.FindAsync(dto.MesaId);
            if (mesa == null)
            {
                return Resultado<ReservaRespostaDto>.Erro("Mesa não encontrada.", TipoErro.NaoEncontrado);
            }

            // Regra 409: Pessoas acima da capacidade da mesa
            if (dto.NumeroPessoas > mesa.CapacidadePessoas)
            {
                return Resultado<ReservaRespostaDto>.Erro($"A mesa selecionada suporta no máximo {mesa.CapacidadePessoas} pessoas.", TipoErro.Conflito);
            }

            var dataUtc = dto.DataHora.UtcDateTime;
            if (dataUtc.Kind != DateTimeKind.Utc) dataUtc = DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc);

            // Regra 409: Mesa ocupada no horário (considerando uma margem de 2 horas para a reserva)
            var inicioIntervalo = dataUtc.AddHours(-2);
            var fimIntervalo = dataUtc.AddHours(2);

            var mesaOcupada = await _context.Reservas
                .AnyAsync(r => r.MesaId == dto.MesaId && r.Status == "Confirmada" && r.DataHoraUtc >= inicioIntervalo && r.DataHoraUtc <= fimIntervalo);

            if (mesaOcupada)
            {
                return Resultado<ReservaRespostaDto>.Erro("A mesa já possui reserva confirmada neste horário.", TipoErro.Conflito);
            }

            var reserva = new Reserva
            {
                MesaId = dto.MesaId,
                UsuarioId = usuarioId,
                NumeroPessoas = dto.NumeroPessoas,
                DataHoraUtc = dataUtc,
                Status = "Confirmada"
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            return Resultado<ReservaRespostaDto>.Ok(new ReservaRespostaDto(
                reserva.Id,
                mesa.Id,
                mesa.NumeroOuNome,
                usuarioId,
                usuario?.Nome ?? "Usuário",
                reserva.NumeroPessoas,
                new DateTimeOffset(reserva.DataHoraUtc),
                reserva.Status
            ));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<ReservaRespostaDto>> ListarReservaAsync(int usuarioId, string papel)
    {
        var query = _context.Reservas
            .Include(r => r.Mesa)
            .Include(r => r.Usuario)
            .AsQueryable();

        // Se não for admin, filtra apenas pelas reservas do próprio usuário
        if (papel != "admin")
        {
            query = query.Where(r => r.UsuarioId == usuarioId);
        }

        return await query.Select(r => new ReservaRespostaDto(
            r.Id,
            r.MesaId,
            r.Mesa!.NumeroOuNome,
            r.UsuarioId,
            r.Usuario!.Nome,
            r.NumeroPessoas,
            new DateTimeOffset(r.DataHoraUtc),
            r.Status
        )).ToListAsync();
    }
}