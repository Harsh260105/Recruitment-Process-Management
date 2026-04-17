
export const LandingPage = () => {
    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-indigo-950 text-slate-100">
            <div className="mx-auto max-w-6xl px-6 py-16 sm:px-8 lg:px-12">
                <header className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
                    <div>
                        <p className="inline-flex rounded-full bg-white/10 px-4 py-1 text-sm font-semibold text-indigo-200 ring-1 ring-white/10">
                            Roima Recruitment System
                        </p>
                        <h1 className="mt-6 text-4xl font-extrabold tracking-tight text-white sm:text-5xl lg:text-6xl">
                            Hire faster. Engage smarter. Scale with confidence.
                        </h1>
                        <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-300 sm:text-xl">
                            Streamline your recruitment workflow with a modern platform built for recruiting teams, hiring managers and candidates.
                        </p>
                        <div className="mt-10 flex flex-col gap-4 sm:flex-row">
                            <a
                                href="/auth/login"
                                className="inline-flex items-center justify-center rounded-full bg-indigo-500 px-6 py-3 text-base font-semibold text-white shadow-lg shadow-indigo-500/20 transition hover:bg-indigo-400"
                            >
                                Log In
                            </a>
                            <a
                                href="/auth/register"
                                className="inline-flex items-center justify-center rounded-full border border-white/20 bg-white/10 px-6 py-3 text-base font-semibold text-white transition hover:bg-white/20"
                            >
                                Create account
                            </a>
                        </div>
                    </div>
                    <div className="rounded-[2rem] border border-white/10 bg-white/5 p-8 shadow-2xl shadow-black/20 backdrop-blur-lg sm:p-10">
                        <div className="space-y-4">
                            <div className="rounded-3xl bg-slate-900/80 p-5">
                                <p className="text-sm uppercase tracking-[0.24em] text-indigo-300">Featured workflow</p>
                                <h2 className="mt-3 text-xl font-semibold text-white">Candidate evaluation in one view</h2>
                                <p className="mt-2 text-sm leading-6 text-slate-400">
                                    Review applications, schedule interviews and manage offers without switching tools.
                                </p>
                            </div>
                            <div className="grid gap-4 sm:grid-cols-2">
                                <div className="rounded-3xl bg-slate-950/70 p-5">
                                    <p className="text-sm font-semibold text-indigo-200">Track every stage</p>
                                    <p className="mt-2 text-sm text-slate-400">Visualize candidate progress from application to onboarding.</p>
                                </div>
                                <div className="rounded-3xl bg-slate-950/70 p-5">
                                    <p className="text-sm font-semibold text-indigo-200">Collaborate with teams</p>
                                    <p className="mt-2 text-sm text-slate-400">Share notes, assign interviews, and keep hiring decisions aligned.</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </header>

                <section className="mt-16 grid gap-8 lg:grid-cols-3">
                    {[
                        {
                            title: 'Smart Applicant Tracking',
                            description: 'Centralize applications, screen candidates faster, and stay organized with automated status updates.',
                        },
                        {
                            title: 'Interview Scheduling',
                            description: 'Coordinate availability, automate reminders, and reduce no-shows with a polished scheduling experience.',
                        },
                        {
                            title: 'Offer Management',
                            description: 'Build offers, send approvals, and close top talent efficiently.',
                        },
                    ].map((item) => (
                        <div key={item.title} className="rounded-3xl border border-white/10 bg-white/5 p-8 shadow-xl shadow-black/10 backdrop-blur-lg transition hover:-translate-y-1 hover:bg-white/10">
                            <h3 className="text-xl font-semibold text-white">{item.title}</h3>
                            <p className="mt-4 text-slate-300">{item.description}</p>
                        </div>
                    ))}
                </section>

                <section className="mt-16 rounded-[2rem] border border-white/10 bg-slate-900/70 p-10 shadow-2xl shadow-black/20 sm:p-12">
                    <div className="grid gap-8 lg:grid-cols-3">
                        <div>
                            <p className="text-sm font-semibold uppercase tracking-[0.28em] text-indigo-300">Why Roima</p>
                            <h2 className="mt-4 text-3xl font-bold text-white">Built for modern recruiting teams.</h2>
                        </div>
                        <div className="space-y-4 text-slate-300">
                            <p>From fast candidate screening to offer approvals, Roima helps you hire with clarity and confidence.</p>
                            <p>Experience a clean interface, intuitive workflows, and built-in collaboration tools that accelerate every hire.</p>
                        </div>
                        <div className="grid gap-4 sm:grid-cols-2">
                            <div className="rounded-3xl bg-slate-950/80 p-5">
                                <p className="text-sm font-semibold text-indigo-200">Save time</p>
                                <p className="mt-2 text-sm text-slate-400">Automate manual tasks so recruiters can focus on people.</p>
                            </div>
                            <div className="rounded-3xl bg-slate-950/80 p-5">
                                <p className="text-sm font-semibold text-indigo-200">Stay organized</p>
                                <p className="mt-2 text-sm text-slate-400">One source of truth for every role and every stage.</p>
                            </div>
                        </div>
                    </div>
                </section>
            </div>
        </div>
    );
}