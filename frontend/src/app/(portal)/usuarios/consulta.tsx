"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Search } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { FiltroMultiple, Insignia, Paginacion, Pestanas, type Tono } from "@/components/modulo";
import { pagina } from "@/lib/menu";
import { sesionVencida } from "@/lib/acceso";
import { desdeUtc, fechaLocal, haceCuanto } from "@/lib/fechas";
import { DialogoEditarUsuario } from "./editar";

// Consulta de usuarios (Figma U1).

type Usuario = {
  id: number;
  nombre: string | null;
  correo: string;
  clave: string | null;
  rol: string;
  externo: boolean;
  grupo: string;
  estado: { tipo: string; hasta: string | null };
  ultimoAcceso: string | null;
};

const POR_PAGINA = 7;
const grupos = ["Todos", "Profesores", "Externos", "Alumnos", "Egresados", "Coordinación"] as const;
type Grupo = (typeof grupos)[number];

const tonoEstado: Record<string, Tono> = { Activo: "verde", Bloqueado: "rojo", Inactivo: "gris", Vence: "azul" };
const etiquetaEstado: Record<string, string> = { Activo: "Activo", Bloqueado: "Bloqueado", Inactivo: "Inactivo", Vence: "Con vencimiento" };

export function EstadoUsuario({ estado }: { estado: Usuario["estado"] }) {
  const texto = estado.tipo === "Vence" && estado.hasta ? `Vence el ${fechaLocal(desdeUtc(estado.hasta))}` : estado.tipo;
  return <Insignia tono={tonoEstado[estado.tipo] ?? "gris"}>{texto}</Insignia>;
}

const normalizar = (s: string) => s.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();

export function ConsultaUsuarios() {
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [esAdmin, setEsAdmin] = useState(false);
  const [grupo, setGrupo] = useState<Grupo>("Todos");
  const [busqueda, setBusqueda] = useState("");
  const [estados, setEstados] = useState<string[]>([]);
  const [pag, setPag] = useState(1);
  const [editando, setEditando] = useState<number | null>(null);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    fetch("/api/usuarios").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setUsuarios(await res.json());
    });
  }, [version]);
  useEffect(() => {
    // El registro de accesos (U4) solo lo ve el administrador: solo para él el nombre es vínculo.
    fetch("/api/auth/me").then(async (res) => res.ok && setEsAdmin((await res.json()).rol === "Administrador"));
  }, []);

  const { conteos, visibles, opciones } = useMemo(() => {
    const texto = normalizar(busqueda.trim());
    const busca = (u: Usuario) => !texto || normalizar(`${u.nombre ?? ""} ${u.correo} ${u.clave ?? ""}`).includes(texto);
    const filtrados = usuarios.filter((u) => busca(u) && (!estados.length || estados.includes(etiquetaEstado[u.estado.tipo])));
    const enGrupo = (u: Usuario) => grupo === "Todos" || u.grupo === grupo;
    const base = usuarios.filter((u) => busca(u) && enGrupo(u));
    return {
      conteos: grupos.map((g) => ({ valor: g, texto: g, conteo: filtrados.filter((u) => g === "Todos" || u.grupo === g).length })),
      visibles: filtrados.filter(enGrupo),
      opciones: Object.entries(etiquetaEstado).map(([e, valor]) => ({ valor, conteo: base.filter((u) => u.estado.tipo === e).length })),
    };
  }, [usuarios, grupo, busqueda, estados]);

  const totalPaginas = Math.max(1, Math.ceil(visibles.length / POR_PAGINA));
  const actual = Math.min(pag, totalPaginas);
  const enPagina = visibles.slice((actual - 1) * POR_PAGINA, actual * POR_PAGINA);
  const p = pagina("/usuarios")!;

  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion={p.descripcion} />
      <section className="rounded-xl border border-portal-borde bg-blanco">
        <Pestanas pestanas={conteos} actual={grupo} onCambio={(g) => { setGrupo(g); setPag(1); }} />
        <div className="flex flex-wrap items-center gap-2.5 px-5 py-4">
          <label className="flex w-[330px] items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5">
            <Search size={18} className="text-portal-placeholder" aria-hidden />
            <input type="text" value={busqueda} onChange={(e) => { setBusqueda(e.target.value); setPag(1); }}
              placeholder="Buscar por nombre, correo o clave" aria-label="Buscar por nombre, correo o clave"
              className="flex-1 text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder" />
          </label>
          <FiltroMultiple etiqueta="Estado" titulo="Estado de la cuenta" opciones={opciones}
            seleccion={estados} onAplicar={(v) => { setEstados(v); setPag(1); }} />
        </div>
        <table className="w-full table-fixed border-t border-portal-borde text-left">
          <colgroup><col className="w-[320px]" /><col className="w-[150px]" /><col className="w-[130px]" /><col className="w-[200px]" /><col /><col className="w-[110px]" /></colgroup>
          <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
            <tr className="border-b border-portal-borde">
              <th className="py-2.5 pl-5 font-semibold">Usuario</th>
              <th className="font-semibold">Rol</th>
              <th className="font-semibold">Clave</th>
              <th className="font-semibold">Estado</th>
              <th className="font-semibold">Último acceso</th>
              <th><span className="sr-only">Acciones</span></th>
            </tr>
          </thead>
          <tbody className="text-sm text-portal-menu-texto">
            {usuarios.length > 0 && visibles.length === 0 && (
              <tr className="border-b border-portal-borde"><td colSpan={6} className="py-4 pl-5 text-portal-placeholder">Ningún usuario coincide con los filtros.</td></tr>
            )}
            {enPagina.map((u) => (
              <tr key={u.id} className="border-b border-portal-borde">
                <td className="py-3 pl-5">
                  {esAdmin ? (
                    <Link href={`/usuarios/${u.id}`} className="font-semibold text-portal-titulo hover:underline">{u.nombre ?? u.correo}</Link>
                  ) : (
                    <p className="font-semibold text-portal-titulo">{u.nombre ?? u.correo}</p>
                  )}
                  <p className="mt-[3px] text-[13px] text-portal-subtexto">{u.correo}</p>
                </td>
                <td><Insignia tono="gris">{u.externo ? "Externo" : u.rol}</Insignia></td>
                <td>{u.clave ?? "—"}</td>
                <td><EstadoUsuario estado={u.estado} /></td>
                <td>{haceCuanto(u.ultimoAcceso)}</td>
                <td className="pr-5 text-right">
                  <button onClick={() => setEditando(u.id)} className="text-[15px] font-semibold text-portal-activo">Editar</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="flex items-center justify-between px-5 py-3.5">
          <p className="text-sm text-portal-subtexto">
            {visibles.length === 0 ? "Mostrando 0 usuarios"
              : `Mostrando ${(actual - 1) * POR_PAGINA + 1}–${(actual - 1) * POR_PAGINA + enPagina.length} de ${visibles.length} usuarios`}
          </p>
          {totalPaginas > 1 && <Paginacion actual={actual} total={totalPaginas} onCambio={setPag} />}
        </div>
      </section>
      <DialogoEditarUsuario id={editando} onCerrar={() => setEditando(null)} onListo={() => setVersion((v) => v + 1)} />
    </>
  );
}
