import Link from "next/link";
import { Suspense } from "react";
import { BarraDeFrase, Encabezado, PieDePagina } from "@/components/institucional";
import { Tarjeta } from "@/components/ui";
import { FormLogin } from "../form-login";

// Portal de egresados (Figma, página Login, 08): entran con su correo personal.
export default function AccesoEgresados() {
  return (
    <>
      <Encabezado titulo="Inicio de Sesión Institucional" />
      <BarraDeFrase />
      <main className="flex-1 px-[92px]">
        <div className="bg-blanco flex flex-wrap items-start gap-[23px] px-[120px] py-[46px]">
          <Tarjeta titulo="Portal de egresados" className="w-[412px]">
            <Suspense>
              <FormLogin egresados />
            </Suspense>
          </Tarjeta>
          <div className="flex min-w-80 flex-1 flex-col gap-3">
            <p>Acceso para egresados de la Maestría en Tecnología y Gestión del Agua.</p>
            <Tarjeta titulo="Egresados">
              <div className="flex flex-col gap-3.5 p-5">
                <p>Tu correo de acceso es el correo personal que registraste. Si no lo recuerdas, contacta a la coordinación.</p>
                <div className="flex flex-col gap-1">
                  <Link href="/recuperar" className="text-vinculo underline">Restablece contraseña</Link>
                  <Link href="/" className="text-vinculo underline">¿Eres alumno o profesor? Inicia sesión aquí</Link>
                </div>
              </div>
            </Tarjeta>
          </div>
        </div>
      </main>
      <PieDePagina />
    </>
  );
}
