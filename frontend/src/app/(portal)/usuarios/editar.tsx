"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Alerta, BotonPortal, CampoPortal, Dialogo, SelectorPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { desdeUtc, fechaLocal, horaLocal } from "@/lib/fechas";

// Editar usuario (Figma U3): correo, rol, cuenta activa, desbloquear y contraseña temporal.

export type DetalleUsuario = {
  id: number;
  nombre: string | null;
  correo: string;
  clave: string | null;
  rol: string;
  activo: boolean;
  externo: boolean;
  institucion: string | null;
  estado: { tipo: string; hasta: string | null };
  bloqueadoHasta: string | null;
  rolesPermitidos: string[];
};

export function useDetalleUsuario(id: number | null, version = 0) {
  const [detalle, setDetalle] = useState<DetalleUsuario | null>(null);
  useEffect(() => {
    if (id === null) return;
    fetch(`/api/usuarios/${id}`).then(async (res) => res.ok && setDetalle(await res.json()));
  }, [id, version]);
  return id === null ? null : detalle?.id === id ? detalle : null;
}

function bloqueo(iso: string) {
  const d = desdeUtc(iso);
  const hoy = d.toDateString() === new Date().toDateString();
  return `Bloqueada por intentos fallidos hasta ${hoy ? "hoy" : `el ${fechaLocal(d)}`} a las ${horaLocal(d)}.`;
}

export function DialogoEditarUsuario({ id, onCerrar, onListo }: { id: number | null; onCerrar: () => void; onListo: () => void }) {
  const [version, setVersion] = useState(0);
  const u = useDetalleUsuario(id, version);
  const [activo, setActivo] = useState<boolean | null>(null);
  const [aviso, setAviso] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);
  const activoActual = activo ?? u?.activo ?? true;

  function cerrar() {
    setActivo(null);
    setAviso(null);
    onCerrar();
  }

  async function accion(ruta: string, exito: string) {
    setEnviando(true);
    const error = await enviarJson(ruta, "POST", {});
    setEnviando(false);
    setAviso(error ? { tipo: "error", texto: error } : { tipo: "exito", texto: exito });
    if (!error) setVersion((v) => v + 1);
  }

  async function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setEnviando(true);
    const error = await enviarJson(`/api/usuarios/${id}`, "PUT", { correo: f.get("correo"), rol: f.get("rol"), activo: activoActual });
    setEnviando(false);
    if (error) return setAviso({ tipo: "error", texto: error });
    cerrar();
    onListo();
  }

  return (
    <Dialogo abierto={id !== null} onCerrar={cerrar} titulo="Editar usuario"
      subtitulo={u ? [u.nombre, u.clave].filter(Boolean).join(" · ") : undefined} ancho="w-[540px]">
      {u && (
        <form onSubmit={guardar} className="flex flex-col gap-5">
          {u.bloqueadoHasta && <Alerta tipo="error">{bloqueo(u.bloqueadoHasta)}</Alerta>}
          <CampoPortal etiqueta="Correo" name="correo" type="email" required maxLength={100} defaultValue={u.correo} />
          <SelectorPortal etiqueta="Rol" name="rol" defaultValue={u.rol} disabled={u.rolesPermitidos.length === 1}>
            {u.rolesPermitidos.map((r) => <option key={r} value={r}>{r}</option>)}
          </SelectorPortal>
          <label className="flex cursor-pointer items-center gap-2.5 text-sm text-portal-menu-texto">
            <button type="button" role="switch" aria-checked={activoActual} onClick={() => setActivo(!activoActual)}
              className={`relative h-6 w-10 rounded-full transition-colors ${activoActual ? "bg-portal-activo" : "bg-portal-borde-campo"}`}>
              <span className={`absolute top-0.5 size-5 rounded-full bg-blanco shadow transition-all ${activoActual ? "left-[18px]" : "left-0.5"}`} />
            </button>
            Cuenta activa
          </label>
          <div className="flex flex-wrap gap-2.5 border-t border-portal-borde pt-4">
            {u.bloqueadoHasta && (
              <BotonPortal type="button" variante="contorno" disabled={enviando} onClick={() => accion(`/api/usuarios/${u.id}/desbloquear`, "Cuenta desbloqueada.")}>
                Desbloquear
              </BotonPortal>
            )}
            <BotonPortal type="button" variante="contorno" disabled={enviando}
              onClick={() => accion(`/api/usuarios/${u.id}/contrasena-temporal`, `Se envió una contraseña temporal a ${u.correo}.`)}>
              Generar contraseña temporal
            </BotonPortal>
          </div>
          {aviso && <Alerta tipo={aviso.tipo}>{aviso.texto}</Alerta>}
          <div className="flex justify-end gap-2.5">
            <BotonPortal type="button" variante="contorno" onClick={cerrar}>Cancelar</BotonPortal>
            <BotonPortal type="submit" disabled={enviando}>Guardar cambios</BotonPortal>
          </div>
        </form>
      )}
    </Dialogo>
  );
}
