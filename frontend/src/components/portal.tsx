"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { ChevronDown, ChevronUp, CircleUserRound, KeyRound, LogOut } from "lucide-react";
import { inicio, menus, miCuenta, pagina, paginaDe, type Item, type Rol } from "@/lib/menu";

type Yo = { nombre: string; rol: Rol; inactividadMinutos: number };

// Recarga completa al salir: con cacheComponents, el router dejaría en memoria las páginas del usuario.
const irA = (ruta: string) => window.location.replace(ruta);
const sesionTerminada = () => irA("/?sesion=terminada");

// Marco de todos los portales (Figma, página Esqueleto): encabezado, menú lateral y control de sesión.
export function MarcoPortal({ children }: { children: ReactNode }) {
  const router = useRouter();
  const ruta = usePathname();
  const [yo, setYo] = useState<Yo | null>(null);

  useEffect(() => {
    fetch("/api/auth/me").then(async (res) => (res.ok ? setYo(await res.json()) : sesionTerminada()));
  }, []);

  // Cada rol solo entra a sus páginas; cualquier otra lo manda a su inicio.
  const entrada = yo && paginaDe(yo.rol, ruta);
  const permitida = !!entrada;
  useEffect(() => {
    if (yo && !permitida) router.replace(inicio(yo.rol));
  }, [yo, permitida, router]);

  useInactividad(yo?.inactividadMinutos);

  if (!yo || !permitida) return null;
  return (
    <div className="flex min-h-screen flex-col bg-blanco">
      <EncabezadoPortal yo={yo} />
      <div className="flex flex-1">
        <MenuLateral rol={yo.rol} ruta={entrada.ruta} />
        <main className="flex flex-1 flex-col gap-5 px-8 pt-7 pb-8">{children}</main>
      </div>
    </div>
  );
}

// Avisa al backend que hay actividad (a lo más una vez por minuto) y cierra la sesión
// en pantalla al cumplirse el tiempo de inactividad, igual que el backend.
function useInactividad(minutos: number | undefined) {
  useEffect(() => {
    if (!minutos) return;
    let vence = setTimeout(sesionTerminada, minutos * 60_000);
    let ultimoAviso = Date.now();
    const actividad = () => {
      clearTimeout(vence);
      vence = setTimeout(sesionTerminada, minutos * 60_000);
      if (Date.now() - ultimoAviso < 60_000) return;
      ultimoAviso = Date.now();
      fetch("/api/auth/actividad", { method: "POST" }).then((res) => res.status === 401 && sesionTerminada());
    };
    const eventos = ["pointerdown", "keydown", "scroll"] as const;
    eventos.forEach((e) => window.addEventListener(e, actividad, { passive: true }));
    return () => {
      clearTimeout(vence);
      eventos.forEach((e) => window.removeEventListener(e, actividad));
    };
  }, [minutos]);
}

const subtitulo: Partial<Record<Rol, string>> = { Coordinador: "Coordinador del Posgrado" };

function EncabezadoPortal({ yo }: { yo: Yo }) {
  return (
    <header className="flex h-[104px] items-center bg-linear-to-r from-portal-degradado-inicio to-portal-degradado-fin text-blanco">
      <div className="flex h-full w-[270px] shrink-0 items-center border-r border-white/8 bg-portal-logo px-6">
        <Image src="/img/logo-uaslp.png" alt="UASLP" width={200} height={78} className="h-[78px] w-[200px] object-contain" priority />
      </div>
      <div className="flex flex-1 flex-col gap-1.5 pl-8">
        <p className="text-[26px] font-bold">PORTAL DEL {yo.rol.toUpperCase()}</p>
        <p className="font-medium">MAESTRÍA EN TECNOLOGÍA Y GESTIÓN DEL AGUA</p>
      </div>
      <div className="flex items-center gap-3.5 pr-8 text-[15px]">
        <CircleUserRound size={46} strokeWidth={1.5} aria-hidden />
        <div className="flex flex-col gap-1">
          <p className="font-semibold">{yo.nombre}</p>
          <p>{subtitulo[yo.rol] ?? yo.rol}</p>
        </div>
      </div>
    </header>
  );
}

const fila = "flex w-full items-center gap-3.5 rounded-lg py-3 pr-3 pl-3.5 text-left";

function MenuLateral({ rol, ruta }: { rol: Rol; ruta: string }) {
  async function salir() {
    await fetch("/api/auth/logout", { method: "POST" });
    irA("/");
  }

  return (
    <nav className="flex w-[270px] shrink-0 flex-col justify-between border-r border-portal-borde px-3.5 py-6">
      <ul className="flex flex-col gap-1.5">
        {menus[rol].map((seccion, i) => (
          <li key={i}>
            {seccion.etiqueta && (
              <p className="pt-4 pb-1 pl-3.5 text-xs font-semibold tracking-[0.48px] text-portal-placeholder uppercase">{seccion.etiqueta}</p>
            )}
            <ul className="flex flex-col gap-1.5">
              {seccion.items.map((item) => (
                <ItemMenu key={item.titulo} item={item} ruta={ruta} />
              ))}
            </ul>
          </li>
        ))}
      </ul>
      <div className="flex flex-col gap-1.5 border-t border-portal-borde pt-1.5">
        <ItemMenu item={{ ...miCuenta, icono: KeyRound }} ruta={ruta} />
        <button onClick={salir} className={`${fila} font-medium text-portal-menu-texto`}>
          <LogOut size={24} className="text-portal-salir" aria-hidden />
          Cerrar sesión
        </button>
      </div>
    </nav>
  );
}

function ItemMenu({ item, ruta }: { item: Item; ruta: string }) {
  const Icono = item.icono;
  const activo = "ruta" in item ? item.ruta === ruta : item.hijos.some((h) => h.ruta === ruta);
  const [abierto, setAbierto] = useState(activo);
  const estilo = activo ? "bg-portal-activo-fondo font-semibold text-portal-activo" : "font-medium text-portal-menu-texto";
  const icono = <Icono size={24} className={activo ? "text-portal-activo" : "text-portal-icono"} aria-hidden />;

  if ("ruta" in item)
    return (
      <li>
        <Link href={item.ruta} className={`${fila} ${estilo}`} aria-current={activo ? "page" : undefined}>
          {icono}
          {item.titulo}
        </Link>
      </li>
    );

  const Flecha = abierto ? ChevronUp : ChevronDown;
  return (
    <li>
      <button onClick={() => setAbierto(!abierto)} className={`${fila} ${estilo}`} aria-expanded={abierto}>
        {icono}
        <span className="flex-1">{item.titulo}</span>
        <Flecha size={18} className={activo ? "text-portal-activo" : "text-portal-icono"} aria-hidden />
      </button>
      {abierto && (
        <ul className="mt-1.5 flex flex-col gap-1.5">
          {item.hijos.map((h) => {
            const actual = h.ruta === ruta;
            return (
              <li key={h.ruta}>
                <Link
                  href={h.ruta}
                  aria-current={actual ? "page" : undefined}
                  className={`flex items-center gap-2 rounded-lg py-[9px] pr-3 text-sm pl-[52px] ${actual ? "bg-portal-activo-fondo font-medium text-portal-activo" : "text-portal-subtexto"}`}
                >
                  {actual && <span className="size-[5px] rounded-full bg-portal-activo" aria-hidden />}
                  {h.titulo}
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </li>
  );
}

// Página de una entrada del menú cuyo módulo todavía no se hace (recuadro de la maqueta Esqueleto).
export function ModuloPendiente({ ruta }: { ruta: string }) {
  const p = pagina(ruta);
  if (!p) return null;
  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion={p.descripcion} />
      <div className="flex flex-1 items-center justify-center rounded-[10px] border-2 border-dashed border-portal-borde text-portal-subtexto">
        Contenido del módulo — se maqueta por separado
      </div>
    </>
  );
}

export function EncabezadoPagina({ titulo, descripcion, children }: { titulo: string; descripcion?: string; children?: ReactNode }) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex flex-col gap-2">
        <h1 className="text-[28px] font-bold text-portal-titulo">{titulo}</h1>
        {descripcion && <p className="text-portal-descripcion">{descripcion}</p>}
      </div>
      {children}
    </div>
  );
}
