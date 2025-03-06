using Balkana.Data.Models;

namespace Balkana.Data.Infrastructure
{
    public class DatabaseBasicEntries
    {
        public static void AddNationalities(IServiceProvider services)
        {
            var data = services.GetRequiredService<ApplicationDbContext>();
            if (data.Nationalities.Any())
            {
                return;
            }

            data.Nationalities.AddRange(new[]
            {
                new Nationality { Name="Bulgaria", FlagURL="https://flagicons.lipis.dev/flags/4x3/bg.svg"},
                new Nationality { Name="Germany", FlagURL="https://flagicons.lipis.dev/flags/4x3/de.svg"},
                new Nationality { Name="Europe", FlagURL="https://flagicons.lipis.dev/flags/4x3/eu.svg"},
                new Nationality { Name="Greece", FlagURL="https://flagicons.lipis.dev/flags/4x3/gr.svg"},
                new Nationality { Name="Albania", FlagURL="https://flagicons.lipis.dev/flags/4x3/al.svg"},
                new Nationality { Name="Armenia", FlagURL="https://flagicons.lipis.dev/flags/4x3/am.svg"},
                new Nationality { Name="Austria", FlagURL="https://flagicons.lipis.dev/flags/4x3/at.svg"},
                new Nationality { Name="Belarus", FlagURL="https://flagicons.lipis.dev/flags/4x3/by.svg"},
                new Nationality { Name="Belgium", FlagURL="https://flagicons.lipis.dev/flags/4x3/be.svg"},
                new Nationality { Name="Bosnia & Herzegovina", FlagURL="https://flagicons.lipis.dev/flags/4x3/ba.svg"},
                new Nationality { Name="Djibouti", FlagURL="https://flagicons.lipis.dev/flags/4x3/dj.svg"},
                new Nationality { Name="Czech Republic", FlagURL="https://flagicons.lipis.dev/flags/4x3/cz.svg"},
                new Nationality { Name="China", FlagURL="https://flagicons.lipis.dev/flags/4x3/cn.svg"},
                new Nationality { Name="Brazil", FlagURL="https://flagicons.lipis.dev/flags/4x3/br.svg"},
                new Nationality { Name="Canada", FlagURL="https://flagicons.lipis.dev/flags/4x3/ca.svg"},
                new Nationality { Name="Croatia", FlagURL="https://flagicons.lipis.dev/flags/4x3/hr.svg"},
                new Nationality { Name="Cyprus", FlagURL="https://flagicons.lipis.dev/flags/4x3/cy.svg"},
                new Nationality { Name="Denmark", FlagURL="https://flagicons.lipis.dev/flags/4x3/dk.svg"},
                new Nationality { Name="Egypt", FlagURL="https://flagicons.lipis.dev/flags/4x3/eg.svg"},
                new Nationality { Name="Estonia", FlagURL="https://flagicons.lipis.dev/flags/4x3/ee.svg"},
                new Nationality { Name="Finland", FlagURL="https://flagicons.lipis.dev/flags/4x3/fi.svg"},
                new Nationality { Name="France", FlagURL="https://flagicons.lipis.dev/flags/4x3/fr.svg"},
                new Nationality { Name="Hungary", FlagURL="https://flagicons.lipis.dev/flags/4x3/hu.svg"},
                new Nationality { Name="Ireland", FlagURL="https://flagicons.lipis.dev/flags/4x3/ie.svg"},
                new Nationality { Name="Israel", FlagURL="https://flagicons.lipis.dev/flags/4x3/il.svg"},
                new Nationality { Name="Italy", FlagURL="https://flagicons.lipis.dev/flags/4x3/it.svg"},
                new Nationality { Name="Iran", FlagURL="https://flagicons.lipis.dev/flags/4x3/ir.svg"},
                new Nationality { Name="Iraq", FlagURL="https://flagicons.lipis.dev/flags/4x3/iq.svg"},
                new Nationality { Name="Jordan", FlagURL="https://flagicons.lipis.dev/flags/4x3/jo.svg"},
                new Nationality { Name="Kazakhstan", FlagURL="https://flagicons.lipis.dev/flags/4x3/kz.svg"},
                new Nationality { Name="Kuwait", FlagURL="https://flagicons.lipis.dev/flags/4x3/kw.svg"},
                new Nationality { Name="Kyrgyzstan", FlagURL="https://flagicons.lipis.dev/flags/4x3/kg.svg"},
                new Nationality { Name="Latvia", FlagURL="https://flagicons.lipis.dev/flags/4x3/lv.svg"},
                new Nationality { Name="Lebanon", FlagURL="https://flagicons.lipis.dev/flags/4x3/lb.svg"},
                new Nationality { Name="Libya", FlagURL="https://flagicons.lipis.dev/flags/4x3/ly.svg"},
                new Nationality { Name="Lichtenstein", FlagURL="https://flagicons.lipis.dev/flags/4x3/li.svg"},
                new Nationality { Name="Lithuania", FlagURL="https://flagicons.lipis.dev/flags/4x3/lt.svg"},
                new Nationality { Name="Luxembourg", FlagURL="https://flagicons.lipis.dev/flags/4x3/lu.svg"},
                new Nationality { Name="Malta", FlagURL="https://flagicons.lipis.dev/flags/4x3/mt.svg"},
                new Nationality { Name="Moldova", FlagURL="https://flagicons.lipis.dev/flags/4x3/md.svg"},
                new Nationality { Name="Monaco", FlagURL="https://flagicons.lipis.dev/flags/4x3/mc.svg"},
                new Nationality { Name="Montenegro", FlagURL="https://flagicons.lipis.dev/flags/4x3/me.svg"},
                new Nationality { Name="Morocco", FlagURL="https://flagicons.lipis.dev/flags/4x3/ma.svg"},
                new Nationality { Name="Netherlands", FlagURL="https://flagicons.lipis.dev/flags/4x3/nl.svg"},
                new Nationality { Name="North Macedonia", FlagURL="https://flagicons.lipis.dev/flags/4x3/mk.svg"},
                new Nationality { Name="Norway", FlagURL="https://flagicons.lipis.dev/flags/4x3/no.svg"},
                new Nationality { Name="Oman", FlagURL="https://flagicons.lipis.dev/flags/4x3/om.svg"},
                new Nationality { Name="Poland", FlagURL="https://flagicons.lipis.dev/flags/4x3/pl.svg"},
                new Nationality { Name="Portugal", FlagURL="https://flagicons.lipis.dev/flags/4x3/pt.svg"},
                new Nationality { Name="Qatar", FlagURL="https://flagicons.lipis.dev/flags/4x3/qa.svg"},
                new Nationality { Name="Romania", FlagURL="https://flagicons.lipis.dev/flags/4x3/ro.svg"},
                new Nationality { Name="Russia", FlagURL="https://flagicons.lipis.dev/flags/4x3/ru.svg"},
                new Nationality { Name="San Marino", FlagURL="https://flagicons.lipis.dev/flags/4x3/sm.svg"},
                new Nationality { Name="Saudi Arabia", FlagURL="https://flagicons.lipis.dev/flags/4x3/sa.svg"},
                new Nationality { Name="Serbia", FlagURL="https://flagicons.lipis.dev/flags/4x3/rs.svg"},
                new Nationality { Name="Slovakia", FlagURL="https://flagicons.lipis.dev/flags/4x3/sk.svg"},
                new Nationality { Name="Slovenia", FlagURL="https://flagicons.lipis.dev/flags/4x3/si.svg"},
                new Nationality { Name="Spain", FlagURL="https://flagicons.lipis.dev/flags/4x3/es.svg"},
                new Nationality { Name="Palestine", FlagURL="https://flagicons.lipis.dev/flags/4x3/ps.svg"},
                new Nationality { Name="Sweden", FlagURL="https://flagicons.lipis.dev/flags/4x3/se.svg"},
                new Nationality { Name="Switzerland", FlagURL="https://flagicons.lipis.dev/flags/4x3/ch.svg"},
                new Nationality { Name="Syria", FlagURL="https://flagicons.lipis.dev/flags/4x3/sy.svg"},
                new Nationality { Name="Tajikistan", FlagURL="https://flagicons.lipis.dev/flags/4x3/tj.svg"},
                new Nationality { Name="Tunisia", FlagURL="https://flagicons.lipis.dev/flags/4x3/tn.svg"},
                new Nationality { Name="Turkmenistan", FlagURL="https://flagicons.lipis.dev/flags/4x3/tm.svg"},
                new Nationality { Name="Turkey", FlagURL="https://flagicons.lipis.dev/flags/4x3/tr.svg"},
                new Nationality { Name="Ukraine", FlagURL="https://flagicons.lipis.dev/flags/4x3/ua.svg"},
                new Nationality { Name="United Arab Emirates", FlagURL="https://flagicons.lipis.dev/flags/4x3/ae.svg"},
                new Nationality { Name="United Kingdom", FlagURL="https://flagicons.lipis.dev/flags/4x3/gb.svg"},
                new Nationality { Name="United States of America", FlagURL="https://flagicons.lipis.dev/flags/4x3/us.svg"},
                new Nationality { Name="Uzbekistan", FlagURL="https://flagicons.lipis.dev/flags/4x3/uz.svg"},
                new Nationality { Name="Yemen", FlagURL="https://flagicons.lipis.dev/flags/4x3/ye.svg"}
                //new Nationality { Name="", FlagURL=""}
            });
            data.SaveChanges();
        }
    }
}
