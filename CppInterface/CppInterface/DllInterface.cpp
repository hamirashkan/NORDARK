#include "DllInterface.h"
#include "stdio.h"
#include <string>

#include "common.h"
#include "comptime.h"
// #include "set.h"
#include "image.h"
// #include "cimage.h"
// #include "dimage.h"
#include "adjacency.h"
#include "gqueue.h"

// Creates Empty Forest

typedef struct _forest {
    Image* P; // predecessor map
    Image* R; // root map
    Image* V; // distance (cost or connectivity) map
} Forest;

Forest* CreateForest(int ncols, int nrows)
{
    Forest* F = (Forest*)calloc(1, sizeof(Forest));

    F->P = CreateImage(ncols, nrows);
    F->R = CreateImage(ncols, nrows);
    F->V = CreateImage(ncols, nrows);

    return(F);
}

// Destroys Forest

void DestroyForest(Forest** F)
{
    Forest* tmp = *F;

    if (tmp != NULL) {
        DestroyImage(&(tmp->P));
        DestroyImage(&(tmp->R));
        DestroyImage(&(tmp->V));
        free(tmp);
        *F = NULL;
    }
}

// Euclidean distance transform
// input: An Image <I>
// output: An Forest <F> ( <P> + <C> + <R> )， Pred, Image R=Rootmap

Forest* DistTrans(Image* I)
{
    int p, q, n = I->ncols * I->nrows, i, tmp;
    Pixel u, v, w;
    AdjRel* A = Circular(1.5), * A4 = Circular(1.0);// an Euclidean adjacency relation <A>, adjacency.c
    Forest* F = CreateForest(I->ncols, I->nrows);// width, hight
    GQueue* Q = CreateGQueue(1024, n, F->V->val);// A priority queue <Q>, gqueue.c

    // Trivial path initialization

    for (p = 0; p < n; p++) {
        u.x = p % I->ncols;
        u.y = p / I->ncols;
        F->V->val[p] = INT_MAX; F->R->val[p] = p; F->P->val[p] = NIL;// set C(p), R(p), P(p) 
        if (I->val[p] != 0) { // p belongs to an object's border
            F->V->val[p] = 0;
            InsertGQueue(&Q, p);
        }
    }

    // Path propagation

    while (!EmptyGQueue(Q)) {
        p = RemoveGQueue(Q);
        u.x = p % I->ncols;
        u.y = p / I->ncols;
        w.x = F->R->val[p] % I->ncols;
        w.y = F->R->val[p] / I->ncols;
        for (i = 1; i < A->n; i++) {
            v.x = u.x + A->dx[i]; // find the coordinates for <q> based on <p> and relative index of <q> in <A>
            v.y = u.y + A->dy[i];
            if (ValidPixel(I, v.x, v.y)) {// judge whether it is within the image
                q = v.x + I->tbrow[v.y];// get q,?
                if (F->V->val[q] > F->V->val[p]) {// C(q) > C(p)
                    tmp = (v.x - w.x) * (v.x - w.x) + (v.y - w.y) * (v.y - w.y);//C'
                    if (tmp < F->V->val[q]) {//if C'<C(q)
                        if (F->V->val[q] != INT_MAX) RemoveGQueueElem(Q, q); // if C(q) not equal infinite, remove <q> from <Q>
                        F->V->val[q] = tmp; F->R->val[q] = F->R->val[p]; F->P->val[q] = p; // Set C(q)<-C', R(q) <-R(p), P(q)<-p
                        InsertGQueue(&Q, q);
                    }
                }
            }
        }
    }

    DestroyGQueue(&Q);
    DestroyAdjRel(&A);
    DestroyAdjRel(&A4);

    return(F);
}

void mainxx()
{
    int p;
    char outfile[100];
    char* file_noext;
    timer* t1 = NULL, * t2 = NULL;
    Image* img, * aux, * sqrt_edt;
    Forest* edt;


    /* The following block must the remarked when using non-linux machines */

    void* trash = malloc(1);
    // struct mallinfo info;
    int MemDinInicial, MemDinFinal;
    // free(trash);
    // info = mallinfo();
    // MemDinInicial = info.uordblks;

    aux = ReadImage("bicycle.pgm");

    // file_noext = strtok(argv[1],".");

    if (MaximumValue(aux) != 1) {
        fprintf(stderr, "Input image must be binary with values 0/1 \n");
        fprintf(stderr, "Assuming lower threshold 100 for this conversion\n");
        img = Threshold(aux, 100, INT_MAX);
        WriteImage(img, "shape.pgm");
    }
    else {
        img = CopyImage(aux);
    }
    DestroyImage(&aux);

    // t1 = Tic();
    edt = DistTrans(img);

    // t2 = Toc();

    // fprintf(stdout,"Euclidian Distance Transform in %f ms\n",CTime(t1,t2));

    sqrt_edt = CreateImage(img->ncols, img->nrows);
    for (p = 0; p < img->ncols * img->nrows; p++)
        sqrt_edt->val[p] = (int)sqrtf(edt->V->val[p]);

    sprintf_s(outfile, "%s_edt.pgm", "bicycle");// argv[1]);
   
    WriteImage(sqrt_edt, outfile);

    DestroyForest(&edt);
    DestroyImage(&img);
    DestroyImage(&sqrt_edt);

    //int ncols = 100;// aux->ncols;// 100;
    //int nrows = 100;// aux->nrows;// 100;
    //sqrt_edt = CreateImage(ncols, nrows);
    //for (p = 0; p < ncols * nrows; p++)
    //    sqrt_edt->val[p] = p;
    //
    //WriteImage(sqrt_edt, "abc.pgm");

    //edt = DistTrans(sqrt_edt);

    //sqrt_edt = CreateImage(ncols, nrows);
    //for (p = 0; p < ncols * nrows; p++)
    //    sqrt_edt->val[p] = (int)sqrtf(edt->V->val[p]);

    //WriteImage(sqrt_edt, "abd.pgm");
}

void(*Debug::Log)(char* message, int iSize);



void maintt()
{
    int p;
    char outfile[100];
    char* file_noext;
    timer* t1 = NULL, * t2 = NULL;
    Image* img, * aux, * sqrt_edt;
    Forest* edt;

    /* The following block must the remarked when using non-linux machines */

    void* trash = malloc(1);
    // struct mallinfo info;
    int MemDinInicial, MemDinFinal;
    // free(trash);
    // info = mallinfo();
    // MemDinInicial = info.uordblks;

    /*----------------------------------------------------------------------*/
    //const char* char_ptr = "bicycle.pgm";

    aux = ReadImage("bicycle.pgm");// argv[1]);

    //// file_noext = strtok(argv[1],".");

    //if (MaximumValue(aux) != 1) {
    //    fprintf(stderr, "Input image must be binary with values 0/1 \n");
    //    fprintf(stderr, "Assuming lower threshold 100 for this conversion\n");
    //    img = Threshold(aux, 100, INT_MAX);
    //    WriteImage(img, "shape.pgm");
    //}
    //else {
    //    img = CopyImage(aux);
    //}
    //DestroyImage(&aux);

    //// t1 = Tic();
    //edt = DistTrans(img);

    //// t2 = Toc();

    //// fprintf(stdout,"Euclidian Distance Transform in %f ms\n",CTime(t1,t2));

    //sqrt_edt = CreateImage(img->ncols, img->nrows);
    //for (p = 0; p < img->ncols * img->nrows; p++)
    //    sqrt_edt->val[p] = (int)sqrtf(edt->V->val[p]);

    //sprintf_s(outfile, "%s_edt.pgm", char_ptr);// argv[1]);
    //
    //WriteImage(sqrt_edt, outfile);

    //DestroyForest(&edt);
    //DestroyImage(&img);
    //DestroyImage(&sqrt_edt);

    ///* The following block must the remarked when using non-linux machines */

    //// info = mallinfo();
    //// MemDinFinal = info.uordblks;
    //// if (MemDinInicial!=MemDinFinal)
    ////   printf("\n\nDinamic memory was not completely deallocated (%d, %d)\n",
    //  //    MemDinInicial,MemDinFinal);
}

int* fnwrapper_intarr()
{
    int* test = new int[3];

    test[0] = 1;
    test[1] = 2;
    test[2] = 3;

    return test;
}

int* add(int* message)
{
    int* test = new int[3];

    test[0] = 10 * message[0];
    test[1] = 20 * message[1];
    test[2] = 30 * message[2];

    return test;
}

// Build 0011, globl Forest
Forest* edt;

int* IFT(int* rawdata, int nrows, int ncols)
{
    int p;
    char outfile[100];
    char* file_noext;
    timer* t1 = NULL, * t2 = NULL;
    Image* img, * aux, * sqrt_edt;

    /* The following block must the remarked when using non-linux machines */

    void* trash = malloc(1);
    // struct mallinfo info;
    int MemDinInicial, MemDinFinal;
    // free(trash);
    // info = mallinfo();
    // MemDinInicial = info.uordblks;

    int  i, n;
    n = ncols * nrows;
    aux = CreateImage(ncols, nrows);
    for (i = 0; i < n; i++)
        aux->val[i] = rawdata[i];

    //aux = ReadImage("bicycle.pgm");

    // file_noext = strtok(argv[1],".");

    if (MaximumValue(aux) != 1) {
        fprintf(stderr, "Input image must be binary with values 0/1 \n");
        fprintf(stderr, "Assuming lower threshold 100 for this conversion\n");
        img = Threshold(aux, 100, INT_MAX);
        WriteImage(img, "shape.pgm");
    }
    else {
        img = CopyImage(aux);
    }
    DestroyImage(&aux);

    // t1 = Tic();
    edt = DistTrans(img);

    // t2 = Toc();

    // fprintf(stdout,"Euclidian Distance Transform in %f ms\n",CTime(t1,t2));

    sqrt_edt = CreateImage(img->ncols, img->nrows);
    for (p = 0; p < img->ncols * img->nrows; p++)
        sqrt_edt->val[p] = (int)sqrtf(edt->V->val[p]);

    sprintf_s(outfile, "%s_edt.pgm", "bicycle");// argv[1]);

    //WriteImage(sqrt_edt, outfile);

    //DestroyForest(&edt);
    DestroyImage(&img);
    //DestroyImage(&sqrt_edt);

    return sqrt_edt->val;
}

// Build 0011, globl Forest
int* GetImage(char x)
{
    if(x == 'P')
        return edt->P->val;
    else if (x == 'R')
        return edt->R->val;
    else if (x == 'V')
        return edt->V->val;
    else
        return edt->V->val;
}

void Release(int* img)
{
    // release img
}

float GetDistance(float x1, float y1, float x2, float y2)
{
	UnityLog("abcGetDistance has been called, hzc");
    mainxx();
	return sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
}


void InitCSharpDelegate(void(*Log)(char* message, int iSize))
{
	Debug::Log = Log;
	UnityLog("Cpp Message:Log has initialized");
}



// image.c

Image* CreateImage(int ncols, int nrows)
{
    Image* img = NULL;
    int i;

    img = (Image*)calloc(1, sizeof(Image));
    if (img == NULL)
    {
        Error(MSG1, "CreateImage");
    }

    img->val = AllocIntArray(nrows * ncols);
    img->tbrow = AllocIntArray(nrows);

    img->tbrow[0] = 0;
    for (i = 1; i < nrows; i++)
        img->tbrow[i] = img->tbrow[i - 1] + ncols;
    img->ncols = ncols;
    img->nrows = nrows;

    return(img);
}

void DestroyImage(Image** img)
{
    Image* aux;

    aux = *img;
    if (aux != NULL)
    {
        if (aux->val != NULL)   free(aux->val);
        if (aux->tbrow != NULL) free(aux->tbrow);
        free(aux);
        *img = NULL;
    }
}

Image* ReadImage(const char* filename)
{
    FILE* fp = NULL;
    errno_t err;// fopen => fopen_s
    unsigned char* value = NULL;
    char type[10];
    int  i, ncols, nrows, n;
    Image* img = NULL;
    char z[256];

    //printf(filename);
    fopen_s(&fp, filename, "rb");
    if (fp == NULL)
    {
        fprintf(stderr, "Cannot open %s\n", filename);
        exit(-1);
    }
    if (fscanf_s(fp, "%s\n", type, sizeof(type)) != 0);//sizeof(type)
    if ((strcmp(type, "P5") == 0))
    {
        NCFgets(z, 255, fp);
        sscanf_s(z, "%d %d\n", &ncols, &nrows);
        n = ncols * nrows;
        NCFgets(z, 255, fp);
        sscanf_s(z, "%d\n", &i);
        fgetc(fp);
        value = (unsigned char*)calloc(n, sizeof(unsigned char));
        if (value != NULL)
        {
            if (fread(value, sizeof(unsigned char), n, fp) != 0);
        }
        else
        {
            fprintf(stderr, "Insufficient memory in ReadImage\n");
            exit(-1);
        }
        fclose(fp);
        img = CreateImage(ncols, nrows);
        for (i = 0; i < n; i++)
            img->val[i] = (int)value[i];
        free(value);
    }
    else
    {
        if ((strcmp(type, "P2") == 0))
        {
            NCFgets(z, 255, fp);
            sscanf_s(z, "%d %d\n", &ncols, &nrows);
            n = ncols * nrows;
            NCFgets(z, 255, fp);
            sscanf_s(z, "%d\n", &i);
            img = CreateImage(ncols, nrows);
            for (i = 0; i < n; i++)
                if (fscanf_s(fp, "%d", &img->val[i]) != 0);
            fclose(fp);
        }
        else
        {
            fprintf(stderr, "Input image must be P2 or P5\n");
            exit(-1);
        }
    }

    return(img);
}

void ExportFile(int* img, int nrows, int ncols, const char* filename)
{
    FILE* fp;
    int i, n, Imax;
    //printf(filename);
    fopen_s(&fp, filename, "wb");
    if (fp == NULL)
    {
        fprintf(stderr, "Cannot open %s\n", filename);
        exit(-1);
    }
    n = ncols * nrows;

    if ((Imax = MaximumVal(img, nrows, ncols)) == INT_MAX)
    {
        Warning("Image with infinity values", "WriteImage");
        Imax = INT_MIN;
        for (i = 0; i < n; i++)
            if ((img[i] > Imax) && (img[i] != INT_MAX))
                Imax = img[i];
        fprintf(fp, "P2\n");
        fprintf(fp, "%d %d\n", ncols, nrows);
        fprintf(fp, "%d\n", Imax + 1);
    }
    else
    {
        fprintf(fp, "P2\n");
        fprintf(fp, "%d %d\n", ncols, nrows);
        if (Imax == 0) Imax++;
        fprintf(fp, "%d\n", Imax);
    }

    for (i = 0; i < n; i++)
    {
        if (img[i] == INT_MAX)
            fprintf(fp, "%d ", Imax + 1);
        else
            fprintf(fp, "%d ", img[i]);
        if (((i + 1) % 17) == 0)
            fprintf(fp, "\n");
    }

    fclose(fp);

}

int MaximumVal(int* img, int nrows, int ncols)
{
    unsigned int i, n, r;
    int max;

    max = img[0];
    n = ncols * nrows - 1;
    r = n % 4;
    n -= r;
    for (i = 1; i < n; i += 4)
    {
        if (img[i] > max)
            max = img[i];
        if (img[i + 1] > max)
            max = img[i + 1];
        if (img[i + 2] > max)
            max = img[i + 2];
        if (img[i + 3] > max)
            max = img[i + 3];
    }
    while (r != 0)
    {
        if (img[i + r - 1] > max)
            max = img[i + r - 1];
        --r;
    }

    return(max);
}

void WriteImage(Image* img, const char* filename)
{
    FILE* fp;
    int i, n, Imax;
    //printf(filename);
    fopen_s(&fp, filename, "wb");
    if (fp == NULL)
    {
        fprintf(stderr, "Cannot open %s\n", filename);
        exit(-1);
    }
    n = img->ncols * img->nrows;
    if ((Imax = MaximumValue(img)) == INT_MAX)
    {
        Warning("Image with infinity values", "WriteImage");
        Imax = INT_MIN;
        for (i = 0; i < n; i++)
            if ((img->val[i] > Imax) && (img->val[i] != INT_MAX))
                Imax = img->val[i];
        fprintf(fp, "P2\n");
        fprintf(fp, "%d %d\n", img->ncols, img->nrows);
        fprintf(fp, "%d\n", Imax + 1);
    }
    else
    {
        fprintf(fp, "P2\n");
        fprintf(fp, "%d %d\n", img->ncols, img->nrows);
        if (Imax == 0) Imax++;
        fprintf(fp, "%d\n", Imax);
    }

    for (i = 0; i < n; i++)
    {
        if (img->val[i] == INT_MAX)
            fprintf(fp, "%d ", Imax + 1);
        else
            fprintf(fp, "%d ", img->val[i]);
        if (((i + 1) % 17) == 0)
            fprintf(fp, "\n");
    }

    fclose(fp);

}


Image* CopyImage(Image* img)
{
    Image* imgc;

    imgc = CreateImage(img->ncols, img->nrows);
    memcpy(imgc->val, img->val, img->ncols * img->nrows * sizeof(int));

    return(imgc);
}

int MinimumValue(Image* img)
{
    int i, min, n;

    n = img->ncols * img->nrows;
    min = img->val[0];
    for (i = 1; i < n; i++)
        if (img->val[i] < min)
            min = img->val[i];

    return(min);
}

int MaximumValue(Image* img)
{
    unsigned int i, n, r;
    int max;

    max = img->val[0];
    n = img->ncols * img->nrows - 1;
    r = n % 4;
    n -= r;
    for (i = 1; i < n; i += 4)
    {
        if (img->val[i] > max)
            max = img->val[i];
        if (img->val[i + 1] > max)
            max = img->val[i + 1];
        if (img->val[i + 2] > max)
            max = img->val[i + 2];
        if (img->val[i + 3] > max)
            max = img->val[i + 3];
    }
    while (r != 0)
    {
        if (img->val[i + r - 1] > max)
            max = img->val[i + r - 1];
        --r;
    }

    return(max);
}

void SetImage(Image* img, int value)
{
    int i, n;
    n = img->ncols * img->nrows;
    for (i = 0; i < n; i++)
    {
        img->val[i] = value;
    }
}

bool ValidPixel(Image* img, int x, int y)
{
    if ((x >= 0) && (x < img->ncols) &&
        (y >= 0) && (y < img->nrows))
        return(true);
    else
        return(false);
}

Image* AddFrame(Image* img, int sz, int value)
{
    Image* fimg;
    int y, * dst, * src, nbytes, offset;

    fimg = CreateImage(img->ncols + (2 * sz), img->nrows + (2 * sz));
    SetImage(fimg, value);
    nbytes = sizeof(int) * img->ncols;
    offset = sz + fimg->tbrow[sz];
    for (y = 0, src = img->val, dst = fimg->val + offset; y < img->nrows; y++, src += img->ncols, dst += fimg->ncols)
    {
        memcpy(dst, src, nbytes);
    }
    return(fimg);
}

Image* RemFrame(Image* fimg, int sz)
{
    Image* img;
    int y, * dst, * src, nbytes, offset;

    img = CreateImage(fimg->ncols - (2 * sz), fimg->nrows - (2 * sz));
    nbytes = sizeof(int) * img->ncols;
    offset = sz + fimg->tbrow[sz];
    for (y = 0, src = fimg->val + offset, dst = img->val; y < img->nrows; y++, src += fimg->ncols, dst += img->ncols)
    {
        memcpy(dst, src, nbytes);
    }
    return(img);
}

Image* Threshold(Image* img, int lower, int higher)
{
    Image* bin = NULL;
    int p, n;

    bin = CreateImage(img->ncols, img->nrows);
    n = img->ncols * img->nrows;
    for (p = 0; p < n; p++)
        if ((img->val[p] >= lower) && (img->val[p] <= higher))
            bin->val[p] = 1;
    return(bin);
}

// adjacency.c

AdjRel* CreateAdjRel(int n)
{
    AdjRel* A = NULL;

    A = (AdjRel*)calloc(1, sizeof(AdjRel));
    if (A != NULL) {
        A->dx = AllocIntArray(n);
        A->dy = AllocIntArray(n);
        A->n = n;
    }
    else {
        Error(MSG1, "CreateAdjRel");
    }

    return(A);
}

void DestroyAdjRel(AdjRel** A)
{
    AdjRel* aux;

    aux = *A;
    if (aux != NULL) {
        if (aux->dx != NULL) free(aux->dx);
        if (aux->dy != NULL) free(aux->dy);
        free(aux);
        *A = NULL;
    }
}


AdjRel* CloneAdjRel(AdjRel* A) {
    AdjRel* C;
    int i;

    C = CreateAdjRel(A->n);
    for (i = 0; i < A->n; i++) {
        C->dx[i] = A->dx[i];
        C->dy[i] = A->dy[i];
    }

    return C;
}


Image* AdjRel2Image(AdjRel* A) {
    int p, i, dx, dy, dxmax, dymax;
    Image* mask;
    Pixel v, u;

    dxmax = dymax = 0;
    for (i = 0; i < A->n; i++) {
        dx = abs(A->dx[i]);
        dy = abs(A->dy[i]);
        if (dx > dxmax)
            dxmax = dx;
        if (dy > dymax)
            dymax = dy;
    }

    mask = CreateImage(dxmax * 2 + 1,
        dymax * 2 + 1);
    u.x = dxmax;
    u.y = dymax;
    for (i = 0; i < A->n; i++) {
        v.x = u.x + A->dx[i];
        v.y = u.y + A->dy[i];

        if (ValidPixel(mask, v.x, v.y)) {
            p = mask->tbrow[v.y] + v.x;
            mask->val[p] = 1;
        }
    }
    return mask;
}


AdjRel* RightSide(AdjRel* A)
{
    AdjRel* R = NULL;
    int i;
    float d;

    /* Let p -> q be an arc represented by the increments dx,dy. Its
       right side is given by the increments Dx = -dy/d + dx/2 and Dy =
       dx/d + dy/2, where d=sqrt(dx�+dy�). */

    R = CreateAdjRel(A->n);
    for (i = 0; i < R->n; i++) {
        d = sqrt(A->dx[i] * A->dx[i] + A->dy[i] * A->dy[i]);
        if (d != 0) {
            R->dx[i] = ROUND(((float)A->dx[i] / 2.0) - ((float)A->dy[i] / d));
            R->dy[i] = ROUND(((float)A->dx[i] / d) + ((float)A->dy[i] / 2.0));
        }
    }

    return(R);
}

AdjRel* LeftSide(AdjRel* A)
{
    AdjRel* L = NULL;
    int i;
    float d;

    /* Let p -> q be an arc represented by the increments dx,dy. Its
       left side is given by the increments Dx = dy/d + dx/2 and Dy =
       -dx/d + dy/2, where d=sqrt(dx�+dy�). */

    L = CreateAdjRel(A->n);
    for (i = 0; i < L->n; i++) {
        d = sqrt(A->dx[i] * A->dx[i] + A->dy[i] * A->dy[i]);
        if (d != 0) {
            L->dx[i] = ROUND(((float)A->dx[i] / 2.0) + ((float)A->dy[i] / d));
            L->dy[i] = ROUND(((float)A->dy[i] / 2) - ((float)A->dx[i] / d));
        }
    }

    return(L);
}

AdjRel* RightSide2(AdjRel* A, float r)
{
    AdjRel* R = NULL;
    int i;
    float d;

    /* Let p -> q be an arc represented by the increments dx,dy. Its
       right side is given by the increments Dx = -dy/d + dx/2 and Dy =
       dx/d + dy/2, where d=sqrt(dx�+dy�). */

    R = CreateAdjRel(A->n);
    for (i = 0; i < R->n; i++) {
        d = sqrt(A->dx[i] * A->dx[i] + A->dy[i] * A->dy[i]);
        if (d != 0) {
            R->dx[i] = ROUND(((float)A->dx[i] / 2.0) - ((float)A->dy[i] / d) * r);
            R->dy[i] = ROUND(((float)A->dx[i] / d) + ((float)A->dy[i] / 2.0) * r);
        }
    }

    return(R);
}

AdjRel* LeftSide2(AdjRel* A, float r)
{
    AdjRel* L = NULL;
    int i;
    float d;

    /* Let p -> q be an arc represented by the increments dx,dy. Its
       left side is given by the increments Dx = dy/d + dx/2 and Dy =
       -dx/d + dy/2, where d=sqrt(dx�+dy�). */

    L = CreateAdjRel(A->n);
    for (i = 0; i < L->n; i++) {
        d = sqrt(A->dx[i] * A->dx[i] + A->dy[i] * A->dy[i]);
        if (d != 0) {
            L->dx[i] = ROUND(((float)A->dx[i] / 2.0) + ((float)A->dy[i] / d) * r);
            L->dy[i] = ROUND(((float)A->dy[i] / 2) - ((float)A->dx[i] / d) * r);
        }
    }

    return(L);
}


AdjRel* Circular(float r)
{
    AdjRel* A = NULL;
    int i, j, k, n, dx, dy, r0, r2, d, i0 = 0;
    float* da, * dr, aux;

    n = 0;

    r0 = (int)r;
    r2 = (int)(r * r);
    for (dy = -r0; dy <= r0; dy++)
        for (dx = -r0; dx <= r0; dx++)
            if (((dx * dx) + (dy * dy)) <= r2)
                n++;

    A = CreateAdjRel(n);
    i = 0;
    for (dy = -r0; dy <= r0; dy++)
        for (dx = -r0; dx <= r0; dx++)
            if (((dx * dx) + (dy * dy)) <= r2) {
                A->dx[i] = dx;
                A->dy[i] = dy;
                if ((dx == 0) && (dy == 0))
                    i0 = i;
                i++;
            }

    /* Set clockwise */

    da = AllocFloatArray(A->n);
    dr = AllocFloatArray(A->n);
    for (i = 0; i < A->n; i++) {
        dx = A->dx[i];
        dy = A->dy[i];
        dr[i] = (float)sqrt((dx * dx) + (dy * dy));
        if (i != i0) {
            da[i] = atan2(-dy, -dx) * 180.0 / PI;
            if (da[i] < 0.0)
                da[i] += 360.0;
        }
    }
    da[i0] = 0.0;
    dr[i0] = 0.0;

    /* place central pixel at first */

    aux = da[i0];
    da[i0] = da[0];
    da[0] = aux;
    aux = dr[i0];
    dr[i0] = dr[0];
    dr[0] = aux;
    d = A->dx[i0];
    A->dx[i0] = A->dx[0];
    A->dx[0] = d;
    d = A->dy[i0];
    A->dy[i0] = A->dy[0];
    A->dy[0] = d;

    /* sort by angle */

    for (i = 1; i < A->n - 1; i++) {
        k = i;
        for (j = i + 1; j < A->n; j++)
            if (da[j] < da[k]) {
                k = j;
            }
        aux = da[i];
        da[i] = da[k];
        da[k] = aux;
        aux = dr[i];
        dr[i] = dr[k];
        dr[k] = aux;
        d = A->dx[i];
        A->dx[i] = A->dx[k];
        A->dx[k] = d;
        d = A->dy[i];
        A->dy[i] = A->dy[k];
        A->dy[k] = d;
    }

    /* sort by radius for each angle */

    for (i = 1; i < A->n - 1; i++) {
        k = i;
        for (j = i + 1; j < A->n; j++)
            if ((dr[j] < dr[k]) && (da[j] == da[k])) {
                k = j;
            }
        aux = dr[i];
        dr[i] = dr[k];
        dr[k] = aux;
        d = A->dx[i];
        A->dx[i] = A->dx[k];
        A->dx[k] = d;
        d = A->dy[i];
        A->dy[i] = A->dy[k];
        A->dy[k] = d;
    }

    free(dr);
    free(da);

    return(A);
}


AdjRel* FastCircular(float r) {
    AdjRel* A = NULL;
    int i, n, dx, dy, r0, r2, d, i0 = 0;

    n = 0;
    r0 = (int)r;
    r2 = (int)(r * r);
    for (dy = -r0; dy <= r0; dy++)
        for (dx = -r0; dx <= r0; dx++)
            if (((dx * dx) + (dy * dy)) <= r2)
                n++;

    A = CreateAdjRel(n);
    i = 0;
    for (dy = -r0; dy <= r0; dy++)
        for (dx = -r0; dx <= r0; dx++)
            if (((dx * dx) + (dy * dy)) <= r2) {
                A->dx[i] = dx;
                A->dy[i] = dy;
                if ((dx == 0) && (dy == 0))
                    i0 = i;
                i++;
            }


    /* place central pixel at first */
    d = A->dx[i0];
    A->dx[i0] = A->dx[0];
    A->dx[0] = d;
    d = A->dy[i0];
    A->dy[i0] = A->dy[0];
    A->dy[0] = d;

    return(A);
}


AdjRel* Horizontal(int r)
{
    AdjRel* A = NULL;
    int i, n, dx;

    n = 2 * r + 1;

    A = CreateAdjRel(n);
    i = 1;
    for (dx = -r; dx <= r; dx++) {
        if (dx != 0) {//if (i != r){
            A->dx[i] = dx;
            A->dy[i] = 0;
            i++;
        }
    }
    /* place the central pixel at first */
    A->dx[0] = 0;
    A->dy[0] = 0;

    return(A);
}

AdjRel* Box(int ncols, int nrows)
{
    AdjRel* A = NULL;
    int i, dx, dy;

    if (ncols % 2 == 0) ncols++;
    if (nrows % 2 == 0) nrows++;

    A = CreateAdjRel(ncols * nrows);
    i = 1;
    for (dy = -nrows / 2; dy <= nrows / 2; dy++) {
        for (dx = -ncols / 2; dx <= ncols / 2; dx++) {
            if ((dx != 0) || (dy != 0)) {
                A->dx[i] = dx;
                A->dy[i] = dy;
                i++;
            }
        }
    }
    /* place the central pixel at first */
    A->dx[0] = 0;
    A->dy[0] = 0;

    return(A);
}

AdjRel* Cross(int ncols, int nrows)
{
    AdjRel* A = NULL;
    int i, dx, dy;

    if (ncols % 2 == 0) ncols++;
    if (nrows % 2 == 0) nrows++;

    //printf("nelems %d\n", ncols + nrows - 1);
    A = CreateAdjRel(ncols + nrows - 1);
    i = 1;
    for (dx = -ncols / 2, dy = 0; dx <= ncols / 2; dx++) {
        if (dx != 0) {
            A->dx[i] = dx;
            A->dy[i] = dy;
            i++;
        }
    }
    //printf("position after horiz %d\n", i);
    for (dy = -nrows / 2, dx = 0; dy <= nrows / 2; dy++) {
        if (dy != 0) {
            A->dx[i] = dx;
            A->dy[i] = dy;
            i++;
        }
    }
    //printf("position after vert %d\n", i);

    /* place the central pixel at first */
    A->dx[0] = 0;
    A->dy[0] = 0;

    return(A);
}

AdjRel* Vertical(int r)
{
    AdjRel* A = NULL;
    int i, n, dy;

    n = 2 * r + 1;

    A = CreateAdjRel(n);
    i = 1;
    for (dy = -r; dy <= r; dy++) {
        if (dy != 0) {//if (i != r){
            A->dy[i] = dy;
            A->dx[i] = 0;
            i++;
        }
    }
    /* place the central pixel at first */
    A->dx[0] = 0;
    A->dy[0] = 0;
    return(A);
}

AdjPxl* AdjPixels(Image* img, AdjRel* A)
{
    AdjPxl* N;
    int i;

    N = (AdjPxl*)calloc(1, sizeof(AdjPxl));
    if (N != NULL) {
        N->dp = AllocIntArray(A->n);
        N->n = A->n;
        for (i = 0; i < N->n; i++)
            N->dp[i] = A->dx[i] + img->ncols * A->dy[i];
    }
    else {
        Error(MSG1, "AdjPixels");
    }

    return(N);
}

void DestroyAdjPxl(AdjPxl** N)
{
    AdjPxl* aux;

    aux = *N;
    if (aux != NULL) {
        if (aux->dp != NULL) free(aux->dp);
        free(aux);
        *N = NULL;
    }
}


int FrameSize(AdjRel* A)
{
    int sz = INT_MIN, i = 0;

    for (i = 0; i < A->n; i++) {
        if (fabs(A->dx[i]) > sz)
            sz = fabs(A->dx[i]);
        if (fabs(A->dy[i]) > sz)
            sz = fabs(A->dy[i]);
    }
    return(sz);
}

AdjRel* ComplAdj(AdjRel* A1, AdjRel* A2)
{
    AdjRel* A;
    int i, j, n;
    char* subset = NULL;

    if (A1->n > A2->n) {
        A = A1;
        A1 = A2;
        A2 = A;
    }

    A = NULL;
    subset = AllocCharArray(A2->n);
    n = 0;
    for (i = 0; i < A1->n; i++)
        for (j = 0; j < A2->n; j++)
            if ((A1->dx[i] == A2->dx[j]) && (A1->dy[i] == A2->dy[j])) {
                subset[j] = 1;
                n++;
                break;
            }
    n = A2->n - n;

    if (n == 0) /* A1 == A2 */
        return(NULL);

    A = CreateAdjRel(n);
    j = 0;
    for (i = 0; i < A2->n; i++)
        if (subset[i] == 0) {
            A->dx[j] = A2->dx[i];
            A->dy[j] = A2->dy[i];
            j++;
        }

    free(subset);

    return(A);
}

AdjRel* ShearedBox(int xsize, int ysize, float Si, float Sj)
{
    int i, usize, vsize;
    Pixel c, p;
    AdjRel* A = NULL;

    // shear: d(Sij+1)

    usize = ROUND(xsize * (fabs(Si) + 1.0));
    vsize = ROUND(ysize * (fabs(Sj) + 1.0));

    A = Box(usize, vsize);

    // position of the center of the voxel
    if (Si > 0) {
        c.x = xsize * (Si + 0.5);
    }
    else {
        c.x = xsize * (0.5);
    }
    if (Sj > 0) {
        c.y = ysize * (Sj + 0.5);
    }
    else {
        c.y = ysize * (0.5);
    }

    i = 0;
    for (p.y = 0; p.y < vsize; p.y++) {
        for (p.x = 0; p.x < usize; p.x++) {
            A->dx[i] = p.x - c.x;
            A->dy[i] = p.y - c.y;
            i++;
        }
    }
    return(A);
}

AdjRel* Ring(float inner_radius, float outer_radius)
{
    AdjRel* A = NULL;
    int i, n, dx, dy, r0i, r2i, r0o, r2o, d;

    if (outer_radius <= inner_radius) {
        Error("outer_radius must be greater than inner_radius", "Ring");
        return(NULL);
    }

    n = 0;
    r0i = (int)inner_radius;
    r2i = (int)(inner_radius * inner_radius);
    r0o = (int)outer_radius;
    r2o = (int)(outer_radius * outer_radius);

    for (dy = -r0o; dy <= r0o; dy++)
        for (dx = -r0o; dx <= r0o; dx++) {
            d = (dx * dx) + (dy * dy);
            if ((d <= r2o) && (d >= r2i))
                n++;
        }

    A = CreateAdjRel(n);
    i = 0;
    for (dy = -r0o; dy <= r0o; dy++)
        for (dx = -r0o; dx <= r0o; dx++) {
            d = (dx * dx) + (dy * dy);
            if ((d <= r2o) && (d >= r2i)) {
                A->dx[i] = dx;
                A->dy[i] = dy;
                i++;
            }
        }

    return(A);
}

AdjRel* KAdjacency()
{
    AdjRel* A = NULL;

    A = CreateAdjRel(4);
    A->dx[0] = 0;  A->dy[0] = -1;
    A->dx[1] = 1;  A->dy[1] = -1;
    A->dx[2] = 0;  A->dy[2] = +1;
    A->dx[3] = 1;  A->dy[3] = +1;
    return(A);
}

// gqueue


GQueue* CreateGQueue(int nbuckets, int nelems, int* value)
{
    GQueue* Q = NULL;

    Q = (GQueue*)malloc(1 * sizeof(GQueue));

    if (Q != NULL)
    {
        Q->C.first = (int*)malloc((nbuckets + 1) * sizeof(int));
        Q->C.last = (int*)malloc((nbuckets + 1) * sizeof(int));
        Q->C.nbuckets = nbuckets;
        if ((Q->C.first != NULL) && (Q->C.last != NULL))
        {
            Q->L.elem = (GQNode*)malloc(nelems * sizeof(GQNode));
            Q->L.nelems = nelems;
            Q->L.value = value;
            if (Q->L.elem != NULL)
            {
                ResetGQueue(Q);
            }
            else
                Error(MSG1, "CreateGQueue");
        }
        else
            Error(MSG1, "CreateGQueue");
    }
    else
        Error(MSG1, "CreateGQueue");

    return(Q);
}

void ResetGQueue(GQueue* Q)
{
    int i;

    Q->C.minvalue = INT_MAX;
    Q->C.maxvalue = INT_MIN;
    SetTieBreak(Q, FIFOBREAK);
    SetRemovalPolicy(Q, MINVALUE);
    for (i = 0; i < Q->C.nbuckets + 1; i++)
        Q->C.first[i] = Q->C.last[i] = NIL;

    for (i = 0; i < Q->L.nelems; i++)
    {
        Q->L.elem[i].next = Q->L.elem[i].prev = NIL;
        Q->L.elem[i].color = WHITE;
    }

}

void DestroyGQueue(GQueue** Q)
{
    GQueue* aux;

    aux = *Q;
    if (aux != NULL)
    {
        if (aux->C.first != NULL) free(aux->C.first);
        if (aux->C.last != NULL) free(aux->C.last);
        if (aux->L.elem != NULL) free(aux->L.elem);
        free(aux);
        *Q = NULL;
    }
}

GQueue* GrowGQueue(GQueue** Q, int nbuckets)
{
    GQueue* Q1 = CreateGQueue(nbuckets, (*Q)->L.nelems, (*Q)->L.value);
    int i, bucket;

    Q1->C.minvalue = (*Q)->C.minvalue;
    Q1->C.maxvalue = (*Q)->C.maxvalue;
    Q1->C.tiebreak = (*Q)->C.tiebreak;
    Q1->C.removal_policy = (*Q)->C.removal_policy;
    for (i = 0; i < (*Q)->C.nbuckets; i++)
        if ((*Q)->C.first[i] != NIL)
        {
            bucket = (*Q)->L.value[(*Q)->C.first[i]] % Q1->C.nbuckets;
            Q1->C.first[bucket] = (*Q)->C.first[i];
            Q1->C.last[bucket] = (*Q)->C.last[i];
        }
    if ((*Q)->C.first[(*Q)->C.nbuckets] != NIL)
    {
        bucket = Q1->C.nbuckets;
        Q1->C.first[bucket] = (*Q)->C.first[(*Q)->C.nbuckets];
        Q1->C.last[bucket] = (*Q)->C.last[(*Q)->C.nbuckets];
    }

    for (i = 0; i < (*Q)->L.nelems; i++)
        Q1->L.elem[i] = (*Q)->L.elem[i];

    DestroyGQueue(Q);
    return(Q1);
}


void InsertGQueue(GQueue** Q, int elem)
{
    int bucket, minvalue = (*Q)->C.minvalue, maxvalue = (*Q)->C.maxvalue;

    if (((*Q)->L.value[elem] == INT_MAX) || ((*Q)->L.value[elem] == INT_MIN))
        bucket = (*Q)->C.nbuckets;
    else
    {
        if ((*Q)->L.value[elem] < minvalue)
            minvalue = (*Q)->L.value[elem];
        if ((*Q)->L.value[elem] > maxvalue)
            maxvalue = (*Q)->L.value[elem];
        if ((maxvalue - minvalue) > ((*Q)->C.nbuckets - 1))
        {
            (*Q) = GrowGQueue(Q, 2 * (maxvalue - minvalue) + 1);
            fprintf(stdout, "Warning: Doubling queue size\n");
        }
        if ((*Q)->C.removal_policy == MINVALUE)
        {
            bucket = (*Q)->L.value[elem] % (*Q)->C.nbuckets;
        }
        else
        {
            bucket = (*Q)->C.nbuckets - 1 - ((*Q)->L.value[elem] % (*Q)->C.nbuckets);
        }
        (*Q)->C.minvalue = minvalue;
        (*Q)->C.maxvalue = maxvalue;
    }
    if ((*Q)->C.first[bucket] == NIL)
    {
        (*Q)->C.first[bucket] = elem;
        (*Q)->L.elem[elem].prev = NIL;
    }
    else
    {
        (*Q)->L.elem[(*Q)->C.last[bucket]].next = elem;
        (*Q)->L.elem[elem].prev = (*Q)->C.last[bucket];
    }

    (*Q)->C.last[bucket] = elem;
    (*Q)->L.elem[elem].next = NIL;
    (*Q)->L.elem[elem].color = GRAY;
}

int RemoveGQueue(GQueue* Q)
{
    int elem = NIL, next, prev;
    int last, current;

    if (Q->C.removal_policy == MINVALUE)
        current = Q->C.minvalue % Q->C.nbuckets;
    else
        current = Q->C.nbuckets - 1 - (Q->C.maxvalue % Q->C.nbuckets);

    /** moves to next element **/

    if (Q->C.first[current] == NIL)
    {
        last = current;

        current = (current + 1) % (Q->C.nbuckets);

        while ((Q->C.first[current] == NIL) && (current != last))
        {
            current = (current + 1) % (Q->C.nbuckets);
        }

        if (Q->C.first[current] != NIL)
        {
            if (Q->C.removal_policy == MINVALUE)
                Q->C.minvalue = Q->L.value[Q->C.first[current]];
            else
                Q->C.maxvalue = Q->L.value[Q->C.first[current]];
        }
        else
        {
            if (Q->C.first[Q->C.nbuckets] != NIL)
            {
                current = Q->C.nbuckets;
                if (Q->C.removal_policy == MINVALUE)
                    Q->C.minvalue = Q->L.value[Q->C.first[current]];
                else
                    Q->C.maxvalue = Q->L.value[Q->C.first[current]];
            }
            else
            {
                Error("GQueue is empty\n", "RemoveGQueue");
            }
        }
    }

    if (Q->C.tiebreak == LIFOBREAK)
    {
        elem = Q->C.last[current];
        prev = Q->L.elem[elem].prev;
        if (prev == NIL)           /* there was a single element in the list */
        {
            Q->C.last[current] = Q->C.first[current] = NIL;
        }
        else
        {
            Q->C.last[current] = prev;
            Q->L.elem[prev].next = NIL;
        }
    }
    else   /* Assume FIFO policy for breaking ties */
    {
        elem = Q->C.first[current];
        next = Q->L.elem[elem].next;
        if (next == NIL)           /* there was a single element in the list */
        {
            Q->C.first[current] = Q->C.last[current] = NIL;
        }
        else
        {
            Q->C.first[current] = next;
            Q->L.elem[next].prev = NIL;
        }
    }

    Q->L.elem[elem].color = BLACK;

    return elem;
}

void RemoveGQueueElem(GQueue* Q, int elem)
{
    int prev, next, bucket;

    if ((Q->L.value[elem] == INT_MAX) || (Q->L.value[elem] == INT_MIN))
        bucket = Q->C.nbuckets;
    else
    {
        if (Q->C.removal_policy == MINVALUE)
            bucket = Q->L.value[elem] % Q->C.nbuckets;
        else
            bucket = Q->C.nbuckets - 1 - (Q->L.value[elem] % Q->C.nbuckets);
    }

    prev = Q->L.elem[elem].prev;
    next = Q->L.elem[elem].next;

    /* if elem is the first element */
    if (Q->C.first[bucket] == elem)
    {
        Q->C.first[bucket] = next;
        if (next == NIL) /* elem is also the last one */
            Q->C.last[bucket] = NIL;
        else
            Q->L.elem[next].prev = NIL;
    }
    else    /* elem is in the middle or it is the last */
    {
        Q->L.elem[prev].next = next;
        if (next == NIL) /* if it is the last */
            Q->C.last[bucket] = prev;
        else
            Q->L.elem[next].prev = prev;
    }

    Q->L.elem[elem].color = BLACK;

}

void UpdateGQueue(GQueue** Q, int elem, int newvalue)
{
    RemoveGQueueElem(*Q, elem);
    (*Q)->L.value[elem] = newvalue;
    InsertGQueue(Q, elem);
}

int EmptyGQueue(GQueue* Q)
{
    int last, current;

    if (Q->C.removal_policy == MINVALUE)
        current = Q->C.minvalue % Q->C.nbuckets;
    else
        current = Q->C.nbuckets - 1 - (Q->C.maxvalue % Q->C.nbuckets);

    if (Q->C.first[current] != NIL)
        return 0;

    last = current;

    current = (current + 1) % (Q->C.nbuckets);

    while ((Q->C.first[current] == NIL) && (current != last))
    {
        current = (current + 1) % (Q->C.nbuckets);
    }

    if (Q->C.first[current] == NIL)
    {
        if (Q->C.first[Q->C.nbuckets] == NIL)
        {
            return(1);
        }
    }

    return (0);
}

// comptime

timer* Tic() { /* It marks the initial time */
    timer* tic = NULL;
    // tic = (timer *)malloc(sizeof(timer));
    // gettimeofday(tic,NULL);
    return(tic);
}

timer* Toc() { /* It marks the final time */
    timer* toc = NULL;
    // toc = (timer *)malloc(sizeof(timer));
    // gettimeofday(toc,NULL);
    return(toc);
}

float CTime(timer* tic, timer* toc) /* It computes the time difference */
{
    float t = 0.0;
    // if ((tic!=NULL)&&(toc!=NULL)){
    //   t = (toc->tv_sec-tic->tv_sec)*1000.0 +
    //     (toc->tv_usec-tic->tv_usec)*0.001;
    //   free(tic);free(toc);
    //   tic=NULL; toc=NULL;
    // }
    return(t);
}

//common.c


char* AllocCharArray(int n)
{
    char* v = NULL;
    v = (char*)calloc(n, sizeof(char));
    if (v == NULL)
        Error(MSG1, "AllocCharArray");
    return(v);
}

uchar* AllocUCharArray(int n)
{
    uchar* v = NULL;
    v = (uchar*)calloc(n, sizeof(uchar));
    if (v == NULL)
        Error(MSG1, "AllocUCharArray");
    return(v);
}

ushort* AllocUShortArray(int n)
{
    ushort* v = NULL;
    v = (ushort*)calloc(n, sizeof(ushort));
    if (v == NULL)
        Error(MSG1, "AllocUShortArray");
    return(v);
}

uint* AllocUIntArray(int n)
{
    uint* v = NULL;
    v = (uint*)calloc(n, sizeof(uint));
    if (v == NULL)
        Error(MSG1, "AllocUIntArray");
    return(v);
}

int* AllocIntArray(int n)
{
    int* v = NULL;
    v = (int*)calloc(n, sizeof(int));
    if (v == NULL)
        Error(MSG1, "AllocIntArray");
    return(v);
}

float* AllocFloatArray(int n)
{
    float* v = NULL;
    v = (float*)calloc(n, sizeof(float));
    if (v == NULL)
        Error(MSG1, "AllocFloatArray");
    return(v);
}

double* AllocDoubleArray(int n)
{
    double* v = NULL;
    v = (double*)calloc(n, sizeof(double));
    if (v == NULL)
        Error(MSG1, "AllocDoubleArray");
    return(v);
}

real* AllocRealArray(int n) {
    real* v = NULL;
    v = (real*)calloc(n, sizeof(real));
    if (v == NULL)
        Error(MSG1, "AllocRealArray");
    return(v);
}

void Error(const char* msg, const char* func) { /* It prints error message and exits
                                    the program. */
    fprintf(stderr, "Error:%s in %s\n", msg, func);
    exit(-1);
}

void Warning(const char* msg, const char* func) { /* It prints warning message and
                                       leaves the routine. */
    fprintf(stdout, "Warning:%s in %s\n", msg, func);

}

void Change(int* a, int* b) { /* It changes content between a and b */
    int c;
    c = *a;
    *a = *b;
    *b = c;
}

void FChange(float* a, float* b) { /* It changes content between floats a and b */
    float c;
    c = *a;
    *a = *b;
    *b = c;
}

int NCFgets(char* s, int m, FILE* f) {
    while (fgets(s, m, f) != NULL)
        if (s[0] != '#') return 1;
    return 0;
}


/**
 * Gera um número inteiro aleatório no intervalo [low,high].
http://www.ime.usp.br/~pf/algoritmos/aulas/random.html
 **/
int RandomInteger(int low, int high) {
    int k;
    double d;

    d = (double)rand() / ((double)RAND_MAX);
    k = d * (high - low);
    return low + k;
}


inline real sqrtreal(real x) {
    int size;

    size = sizeof(real);
    if (size == sizeof(float))
        return sqrtf(x);
    else if (size == sizeof(double))
        return sqrt(x);
    else
        return (real)sqrt((double)x);
}

int SafeMod(int a, int n)
{
    int r = a % n;

    return (r >= 0) ? r : n + r;
}

int IsPowerOf2(int x)
{
    return (x & (x - 1)) == 0;
}