import Link from "next/link";
import { Suspense } from "react";
import { BarraDeFrase, Encabezado, PieDePagina } from "@/components/institucional";
import { Tarjeta } from "@/components/ui";
import { FormLogin } from "./form-login";

// Figma, página Login: 01 Normal, 02 Error de credenciales, 03 Cuenta bloqueada, 04 Sesión terminada.
export default function Login() {
  return (
    <>
      <Encabezado titulo="Inicio de Sesión Institucional" />
      <BarraDeFrase />
      <main className="flex-1 px-[92px]">
        <div className="bg-blanco flex flex-wrap items-start gap-[23px] px-[120px] py-[46px]">
          <Tarjeta titulo="Portal de estudiantes" className="w-[412px]">
            <Suspense>
              <FormLogin />
            </Suspense>
          </Tarjeta>
          <PanelInformativo />
        </div>
      </main>
      <PieDePagina />
    </>
  );
}

const vinculo = "text-vinculo underline";

function PanelInformativo() {
  return (
    <div className="flex min-w-80 flex-1 flex-col gap-3">
      <p>Para el uso del Servicio de autenticación, es indispensable contar con una cuenta institucional:</p>
      <Tarjeta titulo="Estudiantes">
        <div className="flex flex-col gap-3.5 p-5">
          <p>
            <b>Para asesoría:</b> Acuda al Centro de Servicios Escolares de su Escuela o Facultad.
          </p>
          <div className="flex flex-col gap-0.5">
            <p>Si no recuerda su contraseña, la puede cambiar en el siguiente vínculo:</p>
            <Link href="/recuperar" className={vinculo}>Restablece contraseña</Link>
          </div>
        </div>
      </Tarjeta>
      <Tarjeta titulo="Administrativos / Docentes">
        <div className="flex flex-col gap-3.5 p-5">
          <p>Pueden ingresar con su cuenta de correo institucional.</p>
          <p>Para más información, acuda con el Responsable de Tecnología de su Unidad.</p>
          {/* shortcut: faltan las URL institucionales de estos dos vínculos; ponerlas cuando se conozcan. */}
          <div className="flex flex-col gap-1">
            <span className={vinculo}>Habilita correo</span>
            <span className={vinculo}>Cambio de contraseña</span>
          </div>
        </div>
      </Tarjeta>
      <Tarjeta titulo="Egresados">
        <div className="flex flex-col gap-3.5 p-5">
          <p>Si egresaste de la maestría, ingresa con tu correo personal.</p>
          <Link href="/acceso-egresados" className={vinculo}>Acceso de egresados</Link>
        </div>
      </Tarjeta>
    </div>
  );
}
