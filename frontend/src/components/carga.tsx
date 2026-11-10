"use client";

import { FileUp } from "lucide-react";
import { sesionVencida } from "@/lib/acceso";

// Subida de PDF (Figma «Zona de carga», T1 y AT6). El backend vuelve a validar tipo y tamaño.

const MAXIMO = 10 * 1024 * 1024;

export function pdfInvalido(f: File) {
  if (f.type !== "application/pdf") return "El archivo debe ser un PDF.";
  if (f.size > MAXIMO) return "El archivo supera los 10 MB.";
  return null;
}

// Como enviarJson, pero con multipart: devuelve el mensaje de error o null si salió bien.
export async function enviarFormulario(ruta: string, datos: FormData): Promise<string | null> {
  try {
    const res = await fetch(ruta, { method: "POST", body: datos });
    if (res.status === 401) { sesionVencida(); return null; }
    if (res.ok) return null;
    return (await res.json().catch(() => ({}))).mensaje ?? "Revisa los datos e intenta de nuevo.";
  } catch {
    return "No se pudo guardar. Intenta de nuevo.";
  }
}

export function ZonaCarga({ archivo, onElegir, etiquetadoPor }: { archivo: File | null; onElegir: (f: File | undefined) => void; etiquetadoPor?: string }) {
  return (
    <div className="flex items-center gap-3.5 rounded-[10px] border-[1.5px] border-dashed border-portal-borde-campo bg-portal-tabla-cabecera p-4">
      <FileUp size={26} className="shrink-0 text-portal-activo" aria-hidden />
      <div className="flex min-w-0 flex-1 flex-col gap-0.5">
        <span className="truncate text-sm font-semibold text-portal-titulo">{archivo?.name ?? "Selecciona un archivo PDF"}</span>
        <span className="text-[13px] text-portal-subtexto">Tamaño máximo: 10 MB</span>
      </div>
      <label className="cursor-pointer rounded-lg border border-portal-borde-campo bg-blanco py-2.5 pr-[18px] pl-4 text-[15px] font-semibold text-portal-menu-texto has-focus-visible:outline-2 has-focus-visible:outline-portal-activo">
        Examinar
        <input type="file" accept="application/pdf" aria-labelledby={etiquetadoPor} aria-label={etiquetadoPor ? undefined : "Archivo PDF"} className="sr-only"
          onChange={(e) => { onElegir(e.target.files?.[0]); e.target.value = ""; }} />
      </label>
    </div>
  );
}
