import Navbar from './components/Navbar'
import Hero from './components/Hero'
import Modules from './components/Modules'
import TargetUsers from './components/TargetUsers'
import FAQ from './components/FAQ'
import CallToAction from './components/CallToAction'
import Footer from './components/Footer'

export default function App() {
  return (
    <>
      <a href="#main-content" className="skip-link">Przejdź do treści głównej</a>
      <Navbar />
      <main id="main-content" tabIndex={-1}>
        <Hero />
        <Modules />
        <TargetUsers />
        <FAQ />
        <CallToAction />
      </main>
      <Footer />
    </>
  )
}
