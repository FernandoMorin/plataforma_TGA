import Image from "next/image";
import Link from "next/link";
import { Tarjeta } from "@/components/ui";

// Encabezado, barra de frase y pie del estándar institucional (Figma, página Login: 7:383, 7:396, 7:398).

export function Encabezado({ titulo }: { titulo: string }) {
  return (
    <header>
      <div className="bg-primario flex h-[112px] items-center justify-between gap-6 px-[116px] text-blanco">
        <div className="flex items-center gap-4">
          <Image src="/img/logo-uaslp.png" alt="UASLP" width={160} height={63} className="h-[63px] w-40 object-contain" priority />
          <div className="h-[68px] w-0.5 bg-blanco" aria-hidden />
          <p className="text-[21px]">{titulo}</p>
        </div>
        <p className="text-lg font-semibold">Aspirantes | Estudiantes | Egresados | Docentes | Administrativos</p>
      </div>
      <div className="h-2.5 bg-acento" />
    </header>
  );
}

export function BarraDeFrase() {
  return (
    <div className="bg-blanco flex justify-end px-[102px] py-3">
      <p className="font-bold">&quot;Siempre autónoma. Por mi patria educaré.&quot;</p>
    </div>
  );
}

const redes = ["facebook", "twitter", "youtube", "instagram"];
const direccion = [
  "UASLP",
  "Universidad Autónoma de San Luis Potosí",
  "Álvaro Obregón 64, Centro. CP 78000",
  "San Luis Potosí, SLP",
  "444 826 23 00",
  "© 2026 Todos los derechos reservados",
];
const enlaces = ["Universidad", "Oferta Educativa", "Investigación", "Cultura", "Vinculación", "Internacional", "Correo"];

export function PieDePagina() {
  return (
    <footer>
      <div className="h-2.5 bg-acento" />
      <div className="bg-primario flex justify-between px-[103px] py-[30px] text-lg text-blanco">
        <div className="flex flex-col gap-1">
          <div className="flex gap-3 pb-2.5">
            {redes.map((r) => (
              <Image key={r} src={`/img/${r}.svg`} alt={r} width={22} height={22} />
            ))}
          </div>
          {direccion.map((linea) => (
            <p key={linea}>{linea}</p>
          ))}
        </div>
        <ul className="flex flex-col gap-2 font-semibold">
          {enlaces.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      </div>
    </footer>
  );
}

// Panel de la derecha del login y de recuperar acceso (Figma, Login, «Panel informativo» 7:454).
const vinculo = "text-vinculo underline";

export function PanelInformativo() {
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
