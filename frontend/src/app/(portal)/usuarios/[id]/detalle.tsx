"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { BotonPortal, Insignia, type Tono } from "@/components/modulo";
import { desdeUtc, fechaLocal, horaLocal } from "@/lib/fechas";
import { EstadoUsuario } from "../consulta";
import { DialogoEditarUsuario, useDetalleUsuario } from "../editar";

// Usuario con su registro de accesos (Figma U4). Solo para el administrador.

type Acceso = { fecha: string; resultado: string | null; ip: string | null };
const tonoResultado: Record<string, Tono> = { Correcto: "verde", "Contraseña incorrecta": "rojo", Bloqueado: "rojo", "Primer acceso": "azul" };

export function DetalleUsuario() {
  const router = useRouter();
  const id = Number(useParams<{ id: string }>().id);
  const [version, setVersion] = useState(0);
  const u = useDetalleUsuario(Number.isInteger(id) ? id : null, version);
  const [accesos, setAccesos] = useState<Acceso[] | null>(null);
  const [editando, setEditando] = useState(false);

  useEffect(() => {
    fetch(`/api/usuarios/${id}/accesos`).then(async (res) => {
      if (res.status === 403) router.replace("/usuarios");
      else if (res.ok) setAccesos(await res.json());
    });
  }, [id, router]);

  const volver = <Link href="/usuarios" className="text-sm font-semibold text-portal-activo">← Consulta de usuarios</Link>;
  if (!u) return volver;
  const iniciales = (u.nombre ?? u.correo).split(" ").slice(0, 2).map((p) => p[0]).join("").toUpperCase();

  return (
    <>
      {volver}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <span className="flex size-14 items-center justify-center rounded-full bg-portal-activo-fondo text-lg font-bold text-portal-activo">{iniciales}</span>
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center gap-2.5">
              <h1 className="text-2xl font-bold text-portal-titulo">{u.nombre ?? u.correo}</h1>
              <Insignia tono="gris">{u.externo ? "Externo" : u.rol}</Insignia>
              <EstadoUsuario estado={u.estado} />
            </div>
            <p className="text-[15px] text-portal-descripcion">{[u.clave, u.correo, u.institucion].filter(Boolean).join(" · ")}</p>
          </div>
        </div>
        <BotonPortal variante="contorno" onClick={() => setEditando(true)}>Editar</BotonPortal>
      </div>

      <section className="overflow-hidden rounded-xl border border-portal-borde bg-blanco">
        <h2 className="px-5 py-4 text-lg font-semibold text-portal-titulo">Registro de accesos</h2>
        <table className="w-full text-left text-sm text-portal-menu-texto">
          <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
            <tr>
              <th className="w-[260px] py-2.5 pl-5 font-semibold">Fecha y hora</th>
              <th className="w-[220px] font-semibold">Resultado</th>
              <th className="font-semibold">Dirección IP</th>
            </tr>
          </thead>
          <tbody>
            {accesos?.length === 0 && (
              <tr className="border-t border-portal-borde"><td colSpan={3} className="py-4 pl-5 text-portal-placeholder">Sin accesos registrados</td></tr>
            )}
            {accesos?.map((a, i) => {
              const d = desdeUtc(a.fecha);
              return (
                <tr key={i} className="border-t border-portal-borde">
                  <td className="py-3 pl-5">{fechaLocal(d)} · {horaLocal(d)}</td>
                  <td>{a.resultado && <Insignia tono={tonoResultado[a.resultado] ?? "gris"}>{a.resultado}</Insignia>}</td>
                  <td className="text-portal-subtexto">{a.ip ?? "—"}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </section>
      <DialogoEditarUsuario id={editando ? u.id : null} onCerrar={() => setEditando(false)} onListo={() => setVersion((v) => v + 1)} />
    </>
  );
}
